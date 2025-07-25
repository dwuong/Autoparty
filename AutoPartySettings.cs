using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ExileCore.Shared.Attributes;

namespace AutoParty
{
    public class AutoPartySettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(false);

        public TextNode PlayersToInvite { get; set; } = new TextNode("");

        public RangeNode<int> InviteCooldown { get; set; } = new RangeNode<int>(5000, 1000, 30000);

        public RangeNode<int> CheckInterval { get; set; } = new RangeNode<int>(60, 5, 600);
    }
}