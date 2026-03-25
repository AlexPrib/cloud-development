namespace GenerationService;

public record CourseMessage(
    int Id,
    string Name,
    string TeacherFullName,
    DateOnly StartDate,
    DateOnly EndDate,
    int MaxCountStudents,
    int CurrentCountStudents,
    bool HasCertificate,
    decimal Cost,
    int Rating
);
