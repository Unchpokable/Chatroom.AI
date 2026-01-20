using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Chatroom.AI.Models;

/// <summary>
/// Формат истории для отправки в LLM
/// </summary>
public enum HistoryFormat
{
    /// <summary>XML теги: &lt;from name="Имя"&gt;сообщение&lt;/from&gt;</summary>
    Xml,
    /// <summary>Префикс: [Имя]: сообщение</summary>
    Scenario,
    /// <summary>Отдельные user-сообщения с полем name</summary>
    Messages
}

/// <summary>
/// Корневая модель проекта чата для сериализации.
/// </summary>
[XmlRoot("ChatProject")]
public class ChatProject
{
    [XmlAttribute("version")]
    public string Version { get; set; } = "1.0";

    [XmlElement("Meta")]
    public ProjectMeta Meta { get; set; } = new();

    [XmlElement("Settings")]
    public ProjectSettings Settings { get; set; } = new();

    [XmlElement("GlobalSystemPrompt")]
    public string GlobalSystemPrompt { get; set; } = string.Empty;

    [XmlArray("Personas")]
    [XmlArrayItem("Persona")]
    public List<PersonaReference> Personas { get; set; } = new();

    [XmlElement("Summarizer")]
    public SummarizerConfig? Summarizer { get; set; }

    [XmlElement("History")]
    public FileReference? History { get; set; }

    [XmlElement("Summary")]
    public FileReference? Summary { get; set; }
}

/// <summary>
/// Метаданные проекта
/// </summary>
public class ProjectMeta
{
    [XmlElement("Name")]
    public string Name { get; set; } = "Новый чат";

    [XmlElement("Created")]
    public DateTime Created { get; set; } = DateTime.Now;

    [XmlElement("Modified")]
    public DateTime Modified { get; set; } = DateTime.Now;
}

/// <summary>
/// Настройки проекта
/// </summary>
public class ProjectSettings
{
    [XmlElement("ApiKeyEncrypted")]
    public string ApiKeyEncrypted { get; set; } = string.Empty;

    [XmlElement("HistoryFormat")]
    public HistoryFormat HistoryFormat { get; set; } = HistoryFormat.Xml;

    [XmlElement("DefaultLanguage")]
    public Persona.Language DefaultLanguage { get; set; } = Persona.Language.Ru;
}

/// <summary>
/// Ссылка на файл персоны
/// </summary>
public class PersonaReference
{
    [XmlAttribute("file")]
    public string File { get; set; } = string.Empty;
}

/// <summary>
/// Конфигурация суммаризатора
/// </summary>
public class SummarizerConfig
{
    [XmlAttribute("model")]
    public string Model { get; set; } = string.Empty;
}

/// <summary>
/// Ссылка на файл
/// </summary>
public class FileReference
{
    [XmlAttribute("file")]
    public string File { get; set; } = string.Empty;
}
