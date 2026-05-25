using JoyMon.Content;
using JoyMon.Core;

namespace JoyMon.Game;

/// <summary>
/// Resolves map trigger interactions and dynamic walkability for dungeon puzzles.
/// </summary>
public static class MapInteractionService
{
    public static bool IsTileWalkable(MapContent map, PlayerProfile profile, int x, int y)
    {
        if (x < 0 || x >= map.Width || y < 0 || y >= map.Height)
            return false;

        foreach (var trigger in map.Triggers)
        {
            var trackTiles = GetTrackTiles(trigger);
            if (trackTiles.Count > 0
                && !string.IsNullOrEmpty(trigger.SetsFlag)
                && profile.HasFlag(trigger.SetsFlag))
            {
                foreach (var track in trackTiles)
                {
                    if (track.X == x && track.Y == y)
                        return true;
                }
            }

            if (IsRockGate(trigger) && !profile.HasFlag(trigger.RequiredFlag!))
            {
                foreach (var rock in trigger.RockTiles)
                {
                    if (rock.X == x && rock.Y == y)
                        return false;
                }
            }

            if (trackTiles.Count > 0
                && !string.IsNullOrEmpty(trigger.SetsFlag)
                && !profile.HasFlag(trigger.SetsFlag))
            {
                foreach (var track in trackTiles)
                {
                    if (track.X == x && track.Y == y)
                        return false;
                }
            }

            if (trigger.Kind == "lockedDoor" && !string.IsNullOrEmpty(trigger.RequiredFlag))
            {
                if (!profile.HasFlag(trigger.RequiredFlag))
                {
                    foreach (var door in trigger.DoorTiles)
                    {
                        if (door.X == x && door.Y == y)
                            return false;
                    }
                }
            }
        }

        if (map.Layers.Collision[y][x] != 0)
            return false;

        var transition = map.Transitions.FirstOrDefault(t => t.FromTile.X == x && t.FromTile.Y == y);
        if (transition is not null
            && !string.IsNullOrEmpty(transition.RequiredFlag)
            && !profile.HasFlag(transition.RequiredFlag))
        {
            return false;
        }

        return true;
    }

    public static bool TryGetBlockedMessage(MapContent map, PlayerProfile profile, int x, int y, out string message)
    {
        message = string.Empty;

        foreach (var trigger in map.Triggers)
        {
            if (!IsRockGate(trigger) || profile.HasFlag(trigger.RequiredFlag!))
                continue;

            foreach (var rock in trigger.RockTiles)
            {
                if (rock.X == x && rock.Y == y)
                {
                    message = trigger.BlockedMessage ?? "The path is blocked.";
                    return true;
                }
            }
        }

        var trackBlocked = map.Triggers.FirstOrDefault(t =>
            GetTrackTiles(t).Any(tile => tile.X == x && tile.Y == y)
            && !string.IsNullOrEmpty(t.SetsFlag)
            && !profile.HasFlag(t.SetsFlag));

        if (trackBlocked is not null)
        {
            message = trackBlocked.BlockedMessage ?? "The minecart track is raised.";
            return true;
        }

        return false;
    }

    public static bool TryInteract(
        MapContent map,
        PlayerProfile profile,
        int x,
        int y,
        out string message,
        Action? onHealParty = null)
    {
        message = string.Empty;
        var trigger = map.Triggers.FirstOrDefault(t => t.Tile.X == x && t.Tile.Y == y);
        if (trigger is null)
            return false;

        switch (trigger.Kind)
        {
            case "switch":
            case "minecartSwitch":
                if (!string.IsNullOrEmpty(trigger.SetsFlag) && profile.HasFlag(trigger.SetsFlag))
                {
                    message = trigger.AlreadyMessage ?? "The switch is already set.";
                    return true;
                }

                if (!string.IsNullOrEmpty(trigger.SetsFlag))
                    profile.SetFlag(trigger.SetsFlag, true);

                message = trigger.Message ?? "You flipped the switch.";
                return true;

            case "pickup":
                if (!string.IsNullOrEmpty(trigger.SetsFlag) && profile.HasFlag(trigger.SetsFlag))
                {
                    message = trigger.AlreadyMessage ?? "You already have that.";
                    return true;
                }

                if (!string.IsNullOrEmpty(trigger.SetsFlag))
                    profile.SetFlag(trigger.SetsFlag, true);

                message = trigger.Message ?? "You picked something up.";
                return true;

            case "lockedDoor":
                if (!string.IsNullOrEmpty(trigger.RequiredFlag) && !profile.HasFlag(trigger.RequiredFlag))
                {
                    message = trigger.BlockedMessage ?? "It is locked.";
                    return true;
                }

                if (!string.IsNullOrEmpty(trigger.SetsFlag) && profile.HasFlag(trigger.SetsFlag))
                {
                    message = trigger.AlreadyMessage ?? "It is already open.";
                    return true;
                }

                if (!string.IsNullOrEmpty(trigger.SetsFlag))
                    profile.SetFlag(trigger.SetsFlag, true);

                message = trigger.Message ?? "You unlocked the door.";
                return true;

            case "healingSpring":
                onHealParty?.Invoke();
                message = trigger.Message ?? "Your party was restored!";
                return true;

            case "lantern":
                message = trigger.Message ?? "A lantern flickers here.";
                return true;

            default:
                return false;
        }
    }

    private static bool IsRockGate(MapTriggerContent trigger) =>
        trigger.Kind == "rockGate"
        && !string.IsNullOrEmpty(trigger.RequiredFlag)
        && trigger.RockTiles.Count > 0;

    private static List<TransitionTileContent> GetTrackTiles(MapTriggerContent trigger)
    {
        if (trigger.TrackTiles.Count > 0)
            return trigger.TrackTiles;
        if (trigger.Kind is "switch" or "minecartSwitch")
            return trigger.BridgeTiles;
        return trigger.BridgeTiles;
    }
}
