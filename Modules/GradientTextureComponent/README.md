# Gradient Texture Component

A Unity component that allows you to generate gradient sprites at runtime based on a gradient and two points, specifically designed for use with SpriteRenderer components.

## Features

- Create gradient textures and sprites at runtime
- Preview the gradient texture in the editor
- Control the gradient direction with draggable points
- Customize texture size
- Apply the generated sprite directly to the SpriteRenderer
- Regenerate and apply sprite in editor mode with a single click
- Optional preview in the game scene during Play mode

## Usage

1. Add the `GradientTextureComponent` script to any GameObject with a SpriteRenderer component
2. Configure the gradient, start/end points, and texture size in the Inspector
3. See a preview of the texture in both the Inspector and Scene view
4. Click the "Regenerate Preview" button to update the preview and apply it to the SpriteRenderer
5. Optionally enable "Show Preview In Game Scene" to see a preview during Play mode
6. At runtime, the component will automatically generate a sprite and apply it to the SpriteRenderer

## Inspector Properties

- **Gradient**: The color gradient to use for generating the texture
- **Start Point**: The starting point of the gradient (normalized 0-1 coordinates)
- **End Point**: The ending point of the gradient (normalized 0-1 coordinates)
- **Texture Size**: The size of the generated texture (in pixels)
- **Show Preview In Game Scene**: Whether to show a preview in the game scene during Play mode
- **Preview Size**: The size of the preview in the game scene (in pixels)

## Example

```csharp
// Get a reference to the component
GradientTextureComponent gradientTexture = GetComponent<GradientTextureComponent>();

// You can also generate and get the texture programmatically
Texture2D generatedTexture = gradientTexture.GenerateTexture();

// Create a sprite from the texture
Sprite sprite = Sprite.Create(
    generatedTexture,
    new Rect(0, 0, generatedTexture.width, generatedTexture.height),
    new Vector2(0.5f, 0.5f), // Pivot at center
    100f // Pixels per unit
);

// Apply the sprite to a SpriteRenderer
SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
spriteRenderer.sprite = sprite;
```

## Notes

- The component requires a SpriteRenderer component on the same GameObject
- The texture and sprite are generated in memory and not saved to disk
- The gradient direction is defined by the start and end points (normalized 0-1 coordinates)
- The sprite is created with a centered pivot point and 100 pixels per unit
- The editor and game scene previews show the basic texture without shader effects
- The game scene preview is drawn using OnGUI and appears on top of the game view
