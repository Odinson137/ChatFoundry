namespace TelegramService.Services;

/// <summary>
/// Соответствие расширений файлов способу отправки в Telegram (sendPhoto, sendVideo, sendAudio, sendDocument).
/// </summary>
internal static class MediaExtensionMapping
{
    private static readonly HashSet<string> PhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".avi", ".webm", ".mkv"
    };

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".ogg", ".m4a", ".aac"
    };

    public static MediaSendType GetSendTypeByExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension)) return MediaSendType.Document;
        var ext = extension.StartsWith('.') ? extension : "." + extension;
        if (PhotoExtensions.Contains(ext)) return MediaSendType.Photo;
        if (VideoExtensions.Contains(ext)) return MediaSendType.Video;
        if (AudioExtensions.Contains(ext)) return MediaSendType.Audio;
        return MediaSendType.Document;
    }
}

internal enum MediaSendType { Photo, Video, Audio, Document }
