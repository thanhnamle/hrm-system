using System.Net;
using System.Net.Http.Headers;
using HRM.API.IntegrationTests.Infrastructure;

namespace HRM.API.IntegrationTests;

[Collection(ApiCollection.Name)]
public class EmployeeProfileTests(ApiFactory api)
{
    private static readonly byte[] Pdf = [.. "%PDF-1.7\n"u8, .. new byte[2048]];

    private sealed record History(string ChangeType, string Position, string DepartmentName, string? Note);
    private sealed record Qualification(Guid Id, string Type, string Title, string Organization);
    private sealed record Cv(string FileName, int SizeBytes);
    private sealed record Account(string Role, bool IsLockedOut);
    private sealed record Profile(Account? Account, List<string> Skills, List<Qualification> Qualifications, List<History> JobHistory, Cv? Cv);

    [Fact]
    public async Task Seeded_employee_has_a_hired_entry_and_an_account_role()
    {
        var hr = await api.ClientFor(Accounts.Hr);
        var huyId = await EmployeeIdOf(Accounts.Engineer);

        var profile = await (await hr.GetAsync($"/api/employees/{huyId}/profile")).ReadAs<Profile>();

        profile.JobHistory.ShouldHaveSingleItem().ChangeType.ShouldBe("Hired");
        profile.Account!.Role.ShouldBe("Employee");
    }

    [Fact]
    public async Task Job_changes_are_recorded_in_order_and_saved_to_the_database()
    {
        var hr = await api.ClientFor(Accounts.Hr);
        var (id, body) = await NewEmployee(hr);
        var otherDepartment = await EmployeeAccessTests.NewDepartment(hr);

        await (await hr.PutJson($"/api/employees/{id}", body with { Position = "Senior Tester", ChangeNote = "Promotion" }))
            .ShouldHaveStatus(HttpStatusCode.NoContent);
        await (await hr.PutJson($"/api/employees/{id}", body with { Position = "Senior Tester", DepartmentId = otherDepartment }))
            .ShouldHaveStatus(HttpStatusCode.NoContent);
        await (await hr.PutJson($"/api/employees/{id}", body with { Position = "Senior Tester", DepartmentId = otherDepartment }))
            .ShouldHaveStatus(HttpStatusCode.NoContent); // unchanged: no entry
        await (await hr.PostJson($"/api/employees/{id}/terminate", new { note = "Resigned" }))
            .ShouldHaveStatus(HttpStatusCode.NoContent);

        var history = (await (await hr.GetAsync($"/api/employees/{id}/profile")).ReadAs<Profile>()).JobHistory;

        history.Select(h => h.ChangeType).ShouldBe(["Hired", "PositionChanged", "Transferred", "Terminated"]);
        history[1].Note.ShouldBe("Promotion");
        history[2].Position.ShouldBe("Senior Tester");
        history[3].Note.ShouldBe("Resigned");
    }

    [Fact]
    public async Task Skills_and_qualifications_can_be_managed_by_HR()
    {
        var hr = await api.ClientFor(Accounts.Hr);
        var (id, _) = await NewEmployee(hr);

        await (await hr.PutJson($"/api/employees/{id}/skills", new { skills = new[] { "C#", " c# ", "SQL" } }))
            .ShouldHaveStatus(HttpStatusCode.NoContent);
        var added = await hr.PostJson($"/api/employees/{id}/qualifications",
            new { type = "Education", title = "BSc Computer Science", organization = "HCMUT", startDate = "2016-09-01", endDate = "2020-06-30" });
        await added.ShouldHaveStatus(HttpStatusCode.Created);
        var qualificationId = (await added.ReadAs<IdResponse>()).Id;
        await (await hr.PutJson($"/api/employees/{id}/qualifications/{qualificationId}",
                new { type = "Education", title = "BEng Computer Science", organization = "HCMUT" }))
            .ShouldHaveStatus(HttpStatusCode.NoContent);

        var profile = await (await hr.GetAsync($"/api/employees/{id}/profile")).ReadAs<Profile>();
        profile.Skills.ShouldBe(["C#", "SQL"]);
        profile.Qualifications.ShouldHaveSingleItem().Title.ShouldBe("BEng Computer Science");

        await (await hr.DeleteAsync($"/api/employees/{id}/qualifications/{qualificationId}")).ShouldHaveStatus(HttpStatusCode.NoContent);
        (await (await hr.GetAsync($"/api/employees/{id}/profile")).ReadAs<Profile>()).Qualifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task Qualification_input_is_validated()
    {
        var hr = await api.ClientFor(Accounts.Hr);
        var (id, _) = await NewEmployee(hr);

        var response = await hr.PostJson($"/api/employees/{id}/qualifications",
            new { type = "PreviousJob", title = "", organization = "Acme", startDate = "2020-01-01", endDate = "2019-01-01" });

        await response.ShouldHaveStatus(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("Title");
        body.ShouldContain("EndDate");
    }

    [Fact]
    public async Task CV_upload_download_and_replace()
    {
        var hr = await api.ClientFor(Accounts.Hr);
        var (id, _) = await NewEmployee(hr);

        var uploaded = await hr.PutAsync($"/api/employees/{id}/cv", PdfForm(Pdf, "Nam Le CV.pdf"));
        await uploaded.ShouldHaveStatus(HttpStatusCode.OK);
        (await uploaded.ReadAs<Cv>()).FileName.ShouldBe("Nam Le CV.pdf");

        var download = await hr.GetAsync($"/api/employees/{id}/cv");
        await download.ShouldHaveStatus(HttpStatusCode.OK);
        download.Content.Headers.ContentType!.MediaType.ShouldBe("application/pdf");
        download.Content.Headers.ContentDisposition!.DispositionType.ShouldBe("attachment");
        download.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
        (await download.Content.ReadAsByteArrayAsync()).ShouldBe(Pdf);

        // A second upload replaces the first (one CV per employee).
        await (await hr.PutAsync($"/api/employees/{id}/cv", PdfForm([.. Pdf, 1, 2, 3], "v2.pdf"))).ShouldHaveStatus(HttpStatusCode.OK);
        (await (await hr.GetAsync($"/api/employees/{id}/profile")).ReadAs<Profile>()).Cv!.FileName.ShouldBe("v2.pdf");
    }

    [Fact]
    public async Task A_renamed_non_PDF_is_rejected()
    {
        var hr = await api.ClientFor(Accounts.Hr);
        var (id, _) = await NewEmployee(hr);

        var response = await hr.PutAsync($"/api/employees/{id}/cv", PdfForm("MZ not really a pdf"u8.ToArray(), "cv.pdf"));

        await response.ShouldHaveStatus(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Employees_see_their_own_profile_but_cannot_edit_it_or_see_others()
    {
        var employee = await api.ClientFor(Accounts.Engineer2);
        var self = await EmployeeIdOf(Accounts.Engineer2);
        var colleague = await EmployeeIdOf(Accounts.Engineer3);

        await (await employee.GetAsync($"/api/employees/{self}/profile")).ShouldHaveStatus(HttpStatusCode.OK);
        await (await employee.GetAsync($"/api/employees/{colleague}/profile")).ShouldHaveStatus(HttpStatusCode.NotFound);
        await (await employee.PutAsync($"/api/employees/{self}/cv", PdfForm(Pdf, "cv.pdf"))).ShouldHaveStatus(HttpStatusCode.Forbidden);
        await (await employee.PutJson($"/api/employees/{self}/skills", new { skills = new[] { "x" } })).ShouldHaveStatus(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Manager_cannot_open_the_CV_of_another_department()
    {
        var salesPerson = await EmployeeIdOf(Accounts.SalesPerson);

        var response = await (await api.ClientFor(Accounts.EngineeringManager)).GetAsync($"/api/employees/{salesPerson}/cv");

        await response.ShouldHaveStatus(HttpStatusCode.NotFound);
    }

    private sealed record EmployeeBody(
        string FirstName, string LastName, string Email, string Position, Guid DepartmentId, string? ChangeNote = null);

    private static async Task<(Guid Id, EmployeeBody Body)> NewEmployee(HttpClient hr)
    {
        var departmentId = await EmployeeAccessTests.NewDepartment(hr);
        var body = new EmployeeBody("Nam", "Le", $"profile.{Guid.NewGuid():N}@hrm.local", "Tester", departmentId);
        var response = await hr.PostJson("/api/employees", new
        {
            body.FirstName, body.LastName, body.Email, body.Position, body.DepartmentId, hireDate = "2025-01-06",
        });
        await response.ShouldHaveStatus(HttpStatusCode.Created);
        return ((await response.ReadAs<IdResponse>()).Id, body);
    }

    private static MultipartFormDataContent PdfForm(byte[] bytes, string fileName)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    private async Task<Guid> EmployeeIdOf(string email) =>
        (await (await (await api.ClientFor(email)).GetAsync("/api/auth/me")).ReadAs<Me>()).EmployeeId!.Value;
}
