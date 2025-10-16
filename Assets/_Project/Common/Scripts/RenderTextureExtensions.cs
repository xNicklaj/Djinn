using dev.nicklaj.clibs.deblog;
using UnityEngine;

public static class RenderTextureExtensions
{
    /// <summary>
    /// Converts a RenderTexture into a Sprite.
    /// </summary>
    /// <param name="renderTexture">The source RenderTexture.</param>
    /// <returns>A new Sprite created from the RenderTexture's pixel data.</returns>
    public static Sprite ToSprite(this RenderTexture renderTexture)
    {
        if (renderTexture == null)
        {
            Deblog.LogError("RenderTextureExtensions.ToSprite: renderTexture is null!", "Gameplay");
            return null;
        }

        // Backup the current active RenderTexture
        RenderTexture previous = RenderTexture.active;

        // Set this RenderTexture as the active one
        RenderTexture.active = renderTexture;

        // Create a new Texture2D and read pixels from the RenderTexture
        Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        tex.Apply();

        // Restore the previous active RenderTexture
        RenderTexture.active = previous;

        // Create a Sprite from the Texture2D
        return Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f)
        );
    }
}