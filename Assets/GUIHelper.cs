using UnityEngine;

public static class GUIHelper {
	// Progress bar
	public static void ProgressBar(
		string text,
		float progress,
		string tooltip = null,
		ProgressBarStyle style = null
	) {
		if(progress < 0f)
			progress = 0f;
		else if(progress > 1f)
			progress = 1f;
		
		GUI.color = style.backgroundColor;
		GUILayout.Box("", style.backgroundStyle);
		
		var levelRect = GUILayoutUtility.GetLastRect();
		
		if(levelRect.Contains(Event.current.mousePosition))
			GUI.tooltip = tooltip;
		
		var levelFilledRect = new Rect(levelRect);
		levelFilledRect.x += 1;
		levelFilledRect.y += 1;
		levelFilledRect.width -= 2;
		levelFilledRect.height -= 2;
		
		GUI.color = style.backgroundFillColor;
		GUI.Box(levelFilledRect, "", style.backgroundFillStyle);
		
		levelFilledRect.width *= progress;
		
		GUI.color = style.fillColor;
		GUI.Box(levelFilledRect, "", style.fillStyle);
		
		GUI.color = style.textColor;
		GUI.Label(levelRect, text, style.textStyle);
	}
}
