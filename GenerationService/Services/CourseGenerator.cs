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

    private static readonly Faker<Course> _faker = new Faker<Course>("ru")
        .RuleFor(c => c.Id, f => 0)
        .RuleFor(c => c.Name, f => f.PickRandom(_courseNames))
        .RuleFor(c => c.TeacherFullName, f =>
        {
            var person = f.Person;
            var patronymicBase = f.Name.FirstName(Bogus.DataSets.Name.Gender.Male);
            var patronymic = person.Gender == Bogus.DataSets.Name.Gender.Male
                ? $"{patronymicBase}ович"
                : $"{patronymicBase}овна";
            return $"{person.LastName} {person.FirstName} {patronymic}";
        })
        .RuleFor(c => c.StartDate, f => DateOnly.FromDateTime(f.Date.Past(1)))
        .RuleFor(c => c.EndDate, (f, c) => DateOnly.FromDateTime(f.Date.Future(1, c.StartDate.ToDateTime(TimeOnly.MinValue))))
        .RuleFor(c => c.MaxCountStudents, f => f.Random.Int(10, 100))
        .RuleFor(c => c.CurrentCountStudents, (f, c) => f.Random.Int(0, c.MaxCountStudents))
        .RuleFor(c => c.HasCertificate, f => f.Random.Bool())
        .RuleFor(c => c.Cost, f => Math.Round(f.Random.Decimal(0, 50000), 2))
        .RuleFor(c => c.Rating, f => f.Random.Int(1, 5));

    public static Course Generate(int id)
    {
        var course = _faker.Generate();
        course.Id = id;
        return course;
    }
}
