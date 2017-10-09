namespace Torch.API.Managers.Event
{
    public interface IEvent
    {
        /// <summary>
        /// An event that has been cancelled will no be processed in the default manner.
        /// </summary>
        /// <seealso cref="EventHandlerAttribute.SkipCancelled"/>
        bool Cancelled { get; }
    }
}
