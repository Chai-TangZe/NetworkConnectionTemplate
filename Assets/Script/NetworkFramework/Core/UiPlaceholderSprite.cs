using UnityEngine;

/// <summary>
/// 打包后若头像 Sprite 未加载，仍显示占位块，避免 Image.enabled=false 导致「整块消失」。
/// </summary>
public static class UiPlaceholderSprite
{
    static Sprite cached;

    public static Sprite White()
    {
        if (cached != null)
        {
            return cached;
        }

        Texture2D tex = Texture2D.whiteTexture;
        cached = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        return cached;
    }
}
