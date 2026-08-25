# DirectionalUI

DirectionalUI owns spatial focus navigation for irregularly positioned uGUI
`Selectable` objects. It is independent of View stacks and input devices.

## Runtime interface

- `Move(Vector2 direction)` selects the best available object in that direction.
- `Submit()` sends Unity's standard submit event to the current selection.
- `SelectInitial()` restores the configured initial selection, or the first
  available child when none is configured.
- `Refresh()` rediscovers `Selectable` children after UI structure changes.

The game owns input. A keyboard, gamepad, or Input Action adapter translates
device input into calls to `Move` and `Submit`. A scene must provide one active
Unity `EventSystem`; DirectionalUI deliberately does not create or configure it.

The ProjectTentacle reference prototype is under
`Assets/_Project/Prototype/DirectionalUI`.
