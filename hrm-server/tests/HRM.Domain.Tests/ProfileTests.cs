using HRM.Domain.Common;
using HRM.Domain.Entities;
using HRM.Domain.Enums;
using static HRM.Domain.Tests.TestData;

namespace HRM.Domain.Tests;

public class JobHistoryTests
{
    [Fact]
    public void Hire_records_the_first_entry_at_the_hire_date()
    {
        var employee = NewEmployee();

        var entry = employee.JobHistory.ShouldHaveSingleItem();
        entry.ChangeType.ShouldBe(JobChangeType.Hired);
        entry.EffectiveDate.ShouldBe(employee.HireDate);
        entry.Position.ShouldBe("Developer");
        entry.DepartmentId.ShouldBe(employee.DepartmentId);
    }

    [Fact]
    public void New_position_in_the_same_department_is_a_position_change()
    {
        var employee = NewEmployee();

        var entry = employee.ChangeJob("Senior Developer", employee.DepartmentId, Monday, "Promotion");

        entry!.ChangeType.ShouldBe(JobChangeType.PositionChanged);
        entry.Position.ShouldBe("Senior Developer");
        entry.Note.ShouldBe("Promotion");
        employee.Position.ShouldBe("Senior Developer");
        employee.JobHistory.Count.ShouldBe(2);
    }

    [Fact]
    public void New_department_is_a_transfer_with_the_position_from_then_on()
    {
        var employee = NewEmployee();
        var sales = Guid.CreateVersion7();

        var entry = employee.ChangeJob("Account Executive", sales, Monday);

        entry!.ChangeType.ShouldBe(JobChangeType.Transferred);
        entry.DepartmentId.ShouldBe(sales);
        entry.Position.ShouldBe("Account Executive");
    }

    [Fact]
    public void Saving_the_same_job_records_nothing()
    {
        var employee = NewEmployee();

        employee.ChangeJob("  Developer ", employee.DepartmentId, Monday).ShouldBeNull();

        employee.JobHistory.Count.ShouldBe(1);
    }

    [Fact]
    public void Job_change_cannot_predate_the_hire_date()
    {
        var employee = NewEmployee();

        Should.Throw<DomainException>(() => employee.ChangeJob("Lead", employee.DepartmentId, employee.HireDate.AddDays(-1)));
        employee.Position.ShouldBe("Developer");
    }

    [Fact]
    public void Termination_closes_the_history_with_the_last_job()
    {
        var employee = NewEmployee();

        employee.Terminate(Monday, "Resigned");

        var last = employee.JobHistory.Last();
        last.ChangeType.ShouldBe(JobChangeType.Terminated);
        last.EffectiveDate.ShouldBe(Monday);
        last.Position.ShouldBe("Developer");
        last.Note.ShouldBe("Resigned");
    }
}

public class SkillsAndQualificationsTests
{
    [Fact]
    public void Skills_are_trimmed_deduplicated_and_blanks_dropped()
    {
        var employee = NewEmployee();

        employee.SetSkills([" C# ", "c#", "", "SQL", "  "]);

        employee.Skills.ShouldBe(["C#", "SQL"]);
    }

    [Fact]
    public void Too_many_or_too_long_skills_are_rejected()
    {
        var employee = NewEmployee();

        Should.Throw<DomainException>(() => employee.SetSkills(Enumerable.Range(0, Employee.MaxSkills + 1).Select(i => $"Skill {i}")));
        Should.Throw<DomainException>(() => employee.SetSkills([new string('x', Employee.MaxSkillLength + 1)]));
        employee.Skills.ShouldBeEmpty();
    }

    [Fact]
    public void Qualifications_can_be_added_updated_and_removed()
    {
        var employee = NewEmployee();

        var degree = employee.AddQualification(QualificationType.Education, "BSc Computer Science", "HCMUT", new(2016, 9, 1), new(2020, 6, 30), null);
        employee.UpdateQualification(degree.Id, QualificationType.Education, "BEng Computer Science", "HCMUT", new(2016, 9, 1), new(2020, 6, 30), "Honours");

        degree.Title.ShouldBe("BEng Computer Science");
        degree.Description.ShouldBe("Honours");

        employee.RemoveQualification(degree.Id);
        employee.Qualifications.ShouldBeEmpty();
    }

    [Fact]
    public void Qualification_end_cannot_be_before_start()
    {
        Should.Throw<DomainException>(() => NewEmployee().AddQualification(
            QualificationType.PreviousJob, "Developer", "Acme", new(2020, 1, 1), new(2019, 1, 1), null));
    }

    [Fact]
    public void Unknown_qualification_is_rejected()
    {
        Should.Throw<DomainException>(() => NewEmployee().RemoveQualification(Guid.CreateVersion7()));
    }
}

public class EmployeeDocumentTests
{
    private static readonly byte[] Pdf = [.. "%PDF-1.7\n"u8, .. new byte[100]];

    [Fact]
    public void Accepts_a_pdf_and_keeps_only_a_safe_file_name()
    {
        var cv = EmployeeDocument.CreateCv(Guid.CreateVersion7(), @"C:\Users\huy\Docs\..\my ""cv"";.pdf", Pdf);

        cv.FileName.ShouldBe("my cv.pdf");
        cv.ContentType.ShouldBe("application/pdf");
        cv.SizeBytes.ShouldBe(Pdf.Length);
    }

    [Fact]
    public void Adds_the_pdf_extension_when_missing()
    {
        EmployeeDocument.CreateCv(Guid.CreateVersion7(), "resume", Pdf).FileName.ShouldBe("resume.pdf");
    }

    [Fact]
    public void Rejects_a_file_that_is_not_really_a_pdf()
    {
        // Renaming an executable to .pdf doesn't help: the bytes are checked.
        var fake = "MZ\x90\x00 not a pdf"u8.ToArray();
        Should.Throw<DomainException>(() => EmployeeDocument.CreateCv(Guid.CreateVersion7(), "cv.pdf", fake));
    }

    [Fact]
    public void Rejects_empty_and_oversized_files()
    {
        Should.Throw<DomainException>(() => EmployeeDocument.CreateCv(Guid.CreateVersion7(), "cv.pdf", []));
        var huge = new byte[EmployeeDocument.MaxSizeBytes + 1];
        "%PDF-"u8.CopyTo(huge);
        Should.Throw<DomainException>(() => EmployeeDocument.CreateCv(Guid.CreateVersion7(), "cv.pdf", huge));
    }
}
