using Amazon.S3.Model;
using Aspire.Hosting.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Test.Service;

/// <summary>
/// Интеграционные тесты, проверяющие корректную совместную работу всех сервисов бекенда.
/// </summary>
public class IntegrationTests(AppHostFixture fixture) : IClassFixture<AppHostFixture>
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Проверяет, что запрос курса с валидным id возвращает 200 и корректный объект.
    /// </summary>
    [Fact]
    public async Task GetCourse_WithValidId_ReturnsSuccessAndValidCourse()
    {
        using var client = fixture.App.CreateHttpClient("api-gateway", "http");

        using var response = await client.GetAsync("/course?id=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var course = await response.Content.ReadFromJsonAsync<JsonObject>(_jsonOptions);
        Assert.NotNull(course);
        Assert.Equal(1, course["id"]!.GetValue<int>());
        Assert.False(string.IsNullOrEmpty(course["name"]!.GetValue<string>()));
        Assert.False(string.IsNullOrEmpty(course["teacherFullName"]!.GetValue<string>()));
        Assert.True(course["cost"]!.GetValue<decimal>() > 0);
        Assert.True(course["maxCountStudents"]!.GetValue<int>() > 0);
    }

    /// <summary>
    /// Проверяет, что запрос с отрицательным id возвращает 400 BadRequest.
    /// </summary>
    [Fact]
    public async Task GetCourse_WithNegativeId_ReturnsBadRequest()
    {
        using var client = fixture.App.CreateHttpClient("api-gateway", "http");

        using var response = await client.GetAsync("/course?id=-5");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Проверяет, что повторный запрос с тем же id возвращает идентичные данные из Redis-кэша.
    /// </summary>
    [Fact]
    public async Task GetCourse_SameId_ReturnsCachedData()
    {
        var id = Random.Shared.Next(1_000, 9_999);
        using var client = fixture.App.CreateHttpClient("api-gateway", "http");

        var first = await client.GetFromJsonAsync<JsonObject>($"/course?id={id}", _jsonOptions);
        var second = await client.GetFromJsonAsync<JsonObject>($"/course?id={id}", _jsonOptions);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first["name"]!.GetValue<string>(), second["name"]!.GetValue<string>());
        Assert.Equal(first["teacherFullName"]!.GetValue<string>(), second["teacherFullName"]!.GetValue<string>());
        Assert.Equal(first["cost"]!.GetValue<decimal>(), second["cost"]!.GetValue<decimal>());
    }

    /// <summary>
    /// Проверяет, что балансировщик нагрузки успешно направляет запросы к разным репликам.
    /// </summary>
    [Fact]
    public async Task GetCourse_MultipleRequests_LoadBalancerAllSucceed()
    {
        using var client = fixture.App.CreateHttpClient("api-gateway", "http");

        var tasks = Enumerable.Range(0, 6)
            .Select(i => client.GetAsync($"/course?id={600 + i}"));

        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
    }

    /// <summary>
    /// Проверяет, что разные id возвращают разные курсы.
    /// </summary>
    [Fact]
    public async Task GetCourse_DifferentIds_ReturnDifferentCourses()
    {
        using var client = fixture.App.CreateHttpClient("api-gateway", "http");

        var course1 = await client.GetFromJsonAsync<JsonObject>("/course?id=701", _jsonOptions);
        var course2 = await client.GetFromJsonAsync<JsonObject>("/course?id=702", _jsonOptions);

        Assert.NotNull(course1);
        Assert.NotNull(course2);
        Assert.Equal(701, course1["id"]!.GetValue<int>());
        Assert.Equal(702, course2["id"]!.GetValue<int>());
        Assert.NotEqual(course1["name"]!.GetValue<string>(), course2["name"]!.GetValue<string>());
    }

    /// <summary>
    /// Проверяет, что все обязательные поля курса присутствуют и содержат корректные значения.
    /// </summary>
    [Fact]
    public async Task GetCourse_AllFieldsPopulated()
    {
        var id = Random.Shared.Next(10_000, 50_000);
        using var client = fixture.App.CreateHttpClient("api-gateway", "http");

        var course = await client.GetFromJsonAsync<JsonObject>($"/course?id={id}", _jsonOptions);

        Assert.NotNull(course);
        Assert.Equal(id, course["id"]!.GetValue<int>());
        Assert.False(string.IsNullOrEmpty(course["name"]!.GetValue<string>()));
        Assert.False(string.IsNullOrEmpty(course["teacherFullName"]!.GetValue<string>()));
        Assert.True(course["maxCountStudents"]!.GetValue<int>() > 0);
        Assert.True(course["currentCountStudents"]!.GetValue<int>() >= 0);
        Assert.True(course["currentCountStudents"]!.GetValue<int>() <= course["maxCountStudents"]!.GetValue<int>());
        Assert.True(course["cost"]!.GetValue<decimal>() > 0);
        Assert.InRange(course["rating"]!.GetValue<int>(), 1, 10);
        Assert.NotEqual(default, course["startDate"]!.GetValue<string>());
        Assert.NotEqual(default, course["endDate"]!.GetValue<string>());
    }

    /// <summary>
    /// Проверяет сквозной сценарий: Gateway → GenerationService → SQS → FileService → Minio.
    /// </summary>
    [Fact]
    public async Task GetCourse_NewCourse_FileEventuallySavedToMinio()
    {
        var id = Random.Shared.Next(100_000, 200_000);
        var expectedKey = $"course-{id}.json";

        using var client = fixture.App.CreateHttpClient("api-gateway", "http");
        using var response = await client.GetAsync($"/course?id={id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var objects = await fixture.WaitForS3ObjectAsync(expectedKey);

        Assert.NotEmpty(objects);
    }

    /// <summary>
    /// Проверяет, что файл в Minio содержит данные, соответствующие ответу API.
    /// </summary>
    [Fact]
    public async Task GetCourse_S3FileMatchesApiResponse()
    {
        var id = Random.Shared.Next(200_000, 300_000);
        var expectedKey = $"course-{id}.json";

        using var client = fixture.App.CreateHttpClient("api-gateway", "http");
        var apiCourse = await client.GetFromJsonAsync<JsonObject>($"/course?id={id}", _jsonOptions);
        Assert.NotNull(apiCourse);

        var objects = await fixture.WaitForS3ObjectAsync(expectedKey);
        Assert.NotEmpty(objects);

        var getResponse = await fixture.S3Client.GetObjectAsync("courses", expectedKey);
        using var reader = new StreamReader(getResponse.ResponseStream);
        var json = await reader.ReadToEndAsync();
        var savedCourse = JsonNode.Parse(json)?.AsObject();

        Assert.NotNull(savedCourse);
        Assert.Equal(id, savedCourse["id"]!.GetValue<int>());
        Assert.Equal(apiCourse["name"]!.GetValue<string>(), savedCourse["name"]!.GetValue<string>());
        Assert.Equal(apiCourse["teacherFullName"]!.GetValue<string>(), savedCourse["teacherFullName"]!.GetValue<string>());
        Assert.Equal(apiCourse["cost"]!.GetValue<decimal>(), savedCourse["cost"]!.GetValue<decimal>());
        Assert.Equal(apiCourse["rating"]!.GetValue<int>(), savedCourse["rating"]!.GetValue<int>());
    }

    /// <summary>
    /// Проверяет, что повторный запрос (cache hit) не создаёт дубликат файла в Minio.
    /// </summary>
    [Fact]
    public async Task GetCourse_CacheHit_DoesNotDuplicateMinioFile()
    {
        var id = Random.Shared.Next(300_000, 400_000);
        var expectedKey = $"course-{id}.json";

        using var client = fixture.App.CreateHttpClient("api-gateway", "http");

        await client.GetAsync($"/course?id={id}");
        var objectsAfterFirst = await fixture.WaitForS3ObjectAsync(expectedKey);
        Assert.NotEmpty(objectsAfterFirst);

        using var secondResponse = await client.GetAsync($"/course?id={id}");
        secondResponse.EnsureSuccessStatusCode();
        await Task.Delay(TimeSpan.FromSeconds(5));

        var listResponse = await fixture.S3Client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = "courses",
            Prefix = expectedKey
        });

        Assert.Single(listResponse.S3Objects);
    }
}
