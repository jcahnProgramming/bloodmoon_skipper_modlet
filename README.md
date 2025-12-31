# Blood Moon Skipper

A 7 Days to Die modlet that lets players skip blood moons with a democratic voting system featuring a visual UI with YES/NO buttons.

## Features

- ✅ **Visual voting UI** - YES/NO buttons appear on screen for all players
- ✅ **Console commands** - Simple `skipbloodmoon` command
- ✅ **Single player** - Instant skip
- ✅ **Multiplayer** - 30-second democratic vote with UI
- ✅ **Global notifications** - Everyone sees results

## Installation

1. Download the mod
2. Extract `BloodMoonSkipper` folder to your `Mods` directory
3. Restart server/game
4. **For multiplayer**: Server admin must set permissions (see below)

**Mod Folder Location:**
- Windows: `C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die\Mods\`
- Linux: `~/.local/share/7DaysToDie/Mods/`

## Permissions Setup (Multiplayer Only)

For all players to use the commands, server admins must run these commands once:

```
cp add skipbloodmoon 1000
cp add skipbm 1000
cp add votemoon 1000
```

Or add to `serveradmin.xml`:
```xml
<permission cmd="skipbloodmoon" permission_level="1000" />
<permission cmd="skipbm" permission_level="1000" />
<permission cmd="votemoon" permission_level="1000" />
```

## Usage

### Single Player
Open console (F1) and type:
```
skipbloodmoon
```
or
```
skipbm
```
Instantly skips the next blood moon!

### Multiplayer
**Starting a Vote:**
1. Any player opens console (F1) and types: `skipbm`
2. A voting UI with YES/NO buttons appears on everyone's screen
3. Chat message announces the vote
4. Players have 30 seconds to vote

**Voting:**
- **Option 1**: Click the YES or NO button on the UI
- **Option 2**: Open console (F1) and type: `votemoon yes` or `votemoon no`

After 30 seconds, votes are tallied. If YES > NO, blood moon is skipped!

## Example Gameplay

```
[Player opens console and types: skipbm]
[Global Chat]: Vote: Skip the next blood moon? You have 30 seconds to vote!
[UI appears with YES/NO buttons for all players]
[Player2 clicks YES button]
[Player3 opens console, types: votemoon no]
[After 30 seconds]
[Global Chat]: Vote PASSED! (2 Yes, 1 No, 0 Abstained) - Skipping blood moon.
[Global Chat]: Blood moon skipped! Next blood moon on day 21
```

## Commands

| Console Command | Description |
|-----------------|-------------|
| `skipbm` or `skipbloodmoon` | Start a blood moon skip vote |
| `votemoon yes` or `votemoon y` | Vote YES (or click YES button) |
| `votemoon no` or `votemoon n` | Vote NO (or click NO button) |

## Voting Rules

- **Duration**: 30 seconds
- **Pass Condition**: YES votes must exceed NO votes
- **Abstentions**: Players who don't vote don't affect the outcome
- **One Vote Per Person**: Can't change vote after casting
- **Visual + Console**: Vote via UI buttons OR console commands

## Compatibility

- **Game Version**: A21+ (tested on A21 v2.5)
- **Multiplayer**: Yes - works perfectly
- **EAC**: Compatible
- **Other Mods**: Should work with most mods

## Troubleshooting

**"Unknown command" error:**
- Restart the game completely (not just reload save)
- Check that mod folder is in `Mods/BloodMoonSkipper/`

**"Permission denied" error:**
- Server admin needs to set permissions (see Permissions Setup above)

**UI doesn't appear:**
- UI only shows when 2+ players are online
- Single player skips immediately (no voting needed)

**UI appears but only for server owner:**
- This is expected - each player sees their own UI locally
- All players can still vote using console commands

## Building from Source

Requirements:
- Visual Studio 2019+ with C# support
- 7 Days to Die installed

Build script:
```
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

## Version History

- **2.5.0** - Console commands with visual voting UI

## Credits

Created by **Path** (jcahnProgramming)  
GitHub: https://github.com/jcahnProgramming/bloodmoon_skipper_modlet

## License

Free to use and modify. Credit appreciated but not required.
