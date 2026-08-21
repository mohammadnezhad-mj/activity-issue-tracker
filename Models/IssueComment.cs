namespace RayanTask.Models;

public class IssueComment
{
    public int Id { get; set; }
    public int IssueId { get; set; } // Foreign key به SoftwareIssue
    public string CommentText { get; set; } = string.Empty; // متن کامنت
    public string CommenterFirstName { get; set; } = string.Empty; // کامنت کننده - نام
    public string CommenterLastName { get; set; } = string.Empty; // کامنت کننده - نام خانوادگی
    public DateTime CommentedAt { get; set; } = DateTime.Now; // زمان کامنت
}

