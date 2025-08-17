using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PersonNode : Node
{
    /// <summary>”никальный идентификатор</summary>
    public string GUID;

    public Sprite Icon;

    public PersonName PersonName;

    public string Description;
}

public class PersonName
{
    public string GivenName { get; set; }
    public string FamilyName { get; set; }
    public string Patronymic { get; set; }
}

