using UnityEngine;

public class MessageDTO : IMessageDTO
{
    public Author Author { get; private set; }
    public string AuthorName {  get; private set; }
    public string Content { get; private set; }
    public Time Time { get; private set; }

    public MessageDTO(Author author, string authorName, string content, Time time)
    {
        Author = author; AuthorName = authorName; Content = content;
        Time = time;
    }
}
