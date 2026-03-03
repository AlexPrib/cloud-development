namespace GenerationService.Models;

/// <summary>
/// Модель, описывающая учебный курс и его основные метаданные
/// </summary>
public class Course
{
    /// <summary>
    /// Уникальный идентификатор курса
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Название курса
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Полное имя преподавателя курса
    /// </summary>
    public required string TeacherFullName { get; init; }

    /// <summary>
    /// Дата начала курса
    /// </summary>
    public DateOnly StartDate { get; init; }

    /// <summary>
    /// Дата окончания курса
    /// </summary>
    public DateOnly EndDate { get; init; }

    /// <summary>
    /// Максимально допустимое количество студентов, которые могут быть зачислены на курс
    /// </summary>
    public int MaxCountStudents { get; init; }

    /// <summary>
    /// Текущее количество записанных студентов
    /// </summary>
    public int CurrentCountStudents { get; init; }

    /// <summary>
    /// Выдаётся ли по окончании курса сертификат
    /// </summary>
    public bool HasCertificate { get; init; }

    /// <summary>
    /// Стоимость курса
    /// </summary>
    public decimal Cost { get; init; }

    /// <summary>
    /// Рейтинг курса
    /// </summary>
    public int Rating { get; init; }
}
