using DapperDino.Events.CustomEvents;
using DapperDino.Events.UnityEvents;

namespace DapperDino.Events.Listeners
{
    public class HotbarItemListener : BaseGameEventListener<HotbarItem, HotbarItemEvent, UnityHotbarItemEvent> { }
}