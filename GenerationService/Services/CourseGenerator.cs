using Bogus;
using GenerationService.Models;

namespace GenerationService.Services;

public static class CourseGenerator
{
    private static readonly string[] _courseNames =
    [
        "Основы программирования",
        "Математический анализ",
        "Линейная алгебра",
        "Теория вероятностей и математическая статистика",
        "Базы данных и СУБД",
        "Операционные системы",
        "Компьютерные сети",
        "Машинное обучение",
        "Веб-разработка",
        "Алгоритмы и структуры данных",
        "Компьютерная графика",
        "Искусственный интеллект",
        "Облачные технологии",
        "Кибербезопасность",
        "Проектирование программного обеспечения",
        "Объектно-ориентированное программирование",
        "Функциональное программирование",
        "Архитектура компьютеров",
        "Цифровая обработка сигналов",
        "Разработка мобильных приложений"
    ];

    private static readonly string[] _malePatronymics =
    [
        "Иванович",
        "Петрович",
        "Сидорович",
        "Алексеевич"
    ];

    private static readonly string[] _femalePatronymics =
    [
        "Ивановна",
        "Петровна",
        "Сидоровна",
        "Алексеевна"
    ];

    public static Course Generate(int id)
    {
        var faker = new Faker("ru") { Random = new Randomizer(id) };

        var person = faker.Person;
        var patronymic = person.Gender == Bogus.DataSets.Name.Gender.Male
            ? faker.PickRandom(_malePatronymics)
            : faker.PickRandom(_femalePatronymics);

        var maxStudents = faker.Random.Int(10, 100);
        var currentStudents = faker.Random.Int(0, maxStudents);

        var startDateTime = faker.Date.Past(1);
        var endDateTime = faker.Date.Future(1, startDateTime);

        return new Course
        {
            Id = id,
            Name = faker.PickRandom(_courseNames),
            TeacherFullName = $"{person.LastName} {person.FirstName} {patronymic}",
            StartDate = DateOnly.FromDateTime(startDateTime),
            EndDate = DateOnly.FromDateTime(endDateTime),
            MaxStudents = maxStudents,
            CurrentStudents = currentStudents,
            HasCertificate = faker.Random.Bool(),
            Cost = Math.Round(faker.Random.Decimal(0, 50000), 2),
            Rating = faker.Random.Int(1, 5)
        };
    }
}
