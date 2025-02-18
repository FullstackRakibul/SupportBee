﻿namespace SupportApp.DTO
{
    public class CodeSnippetDto
    {
        public string? EmpId { get; set; }
        public string Title { get; set; }
        public string Language { get; set; }
        public string? Description { get; set; }
        public string? SoftwareType { get; set; }
        public string Code { get; set; }
        public string? Author { get; set; }
        public bool? IsPublic { get; set; }
        public bool? IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
