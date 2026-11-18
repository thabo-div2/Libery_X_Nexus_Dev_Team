using Shared.DTOs.Workflow;
namespace frontend.Services;
public class WorkflowService(HttpClient http)
{
    public Task<(List<AdvisorTaskDto>? Value, string? Error)> GetTasksAsync(int? clientId = null) => ApiErrors.SendAsync<List<AdvisorTaskDto>>(() => http.GetAsync(clientId.HasValue ? $"workflow/tasks?clientId={clientId.Value}" : "workflow/tasks"));
    public Task<(List<AdvisorTaskDto>? Value, string? Error)> GetOpenTasksAsync() => ApiErrors.SendAsync<List<AdvisorTaskDto>>(() => http.GetAsync("workflow/tasks?completed=false"));
    public Task<(List<AdvisorTaskDto> Items, int Total, string? Error)> GetCompletedTasksAsync(int skip, int take) => ApiErrors.SendPageAsync<AdvisorTaskDto>(() => http.GetAsync($"workflow/tasks?completed=true&skip={skip}&take={take}"));
    public Task<(AdvisorTaskDto? Value, string? Error)> SaveTaskAsync(int? id, TaskInput input) => ApiErrors.SendAsync<AdvisorTaskDto>(() => id.HasValue ? http.PutAsJsonAsync($"workflow/tasks/{id}", input) : http.PostAsJsonAsync("workflow/tasks", input));
    public Task<string?> DeleteTaskAsync(int id) => ApiErrors.SendAsync(() => http.DeleteAsync($"workflow/tasks/{id}"));
    public Task<(DocumentReviewDto? Value, string? Error)> GetReviewAsync(int id) => ApiErrors.SendAsync<DocumentReviewDto>(() => http.GetAsync($"workflow/documents/{id}/review"));
    public Task<(DocumentReviewDto? Value, string? Error)> ReviewAsync(int id, DocumentReviewInput input) => ApiErrors.SendAsync<DocumentReviewDto>(() => http.PutAsJsonAsync($"workflow/documents/{id}/review", input));
    public Task<(MeetingSummaryDto? Value, string? Error)> GetSummaryAsync(int id) => ApiErrors.SendAsync<MeetingSummaryDto>(() => http.GetAsync($"workflow/meetings/{id}/summary"));
    public Task<(MeetingSummaryDto? Value, string? Error)> SaveSummaryAsync(int id, MeetingSummaryInput input) => ApiErrors.SendAsync<MeetingSummaryDto>(() => http.PutAsJsonAsync($"workflow/meetings/{id}/summary", input));
    public Task<(OnboardingDto? Value, string? Error)> OnboardingAsync(int clientId) => ApiErrors.SendAsync<OnboardingDto>(() => http.GetAsync($"workflow/clients/{clientId}/onboarding"));
}
