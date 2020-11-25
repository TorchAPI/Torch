using System;

namespace Torch.Utils
{
    /// <summary>
    /// Attribute used to indicate that the the given field, of type <![CDATA[Func<ReflectedEventReplacer>]]>, should be filled with
    /// a function used to create a new event replacer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ReflectedEventReplaceAttribute : Attribute
    {
        /// <summary>
        /// Type that the event is declared in
        /// </summary>
        public Type EventDeclaringType { get; set; }
        /// <summary>
        /// Name of the event
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// Type that the method to replace is declared in
        /// </summary>
        public Type TargetDeclaringType { get; set; }
        /// <summary>
        /// Name of the method to replace
        /// </summary>
        public string TargetName { get; set; }
        /// <summary>
        /// Optional parameters of the method to replace.  Null to ignore.
        /// </summary>
        public Type[] TargetParameters { get; set; } = null;

        /// <summary>
        /// Creates a reflected event replacer attribute to, for the event defined as eventName in eventDeclaringType,
        /// replace the method defined as targetName in targetDeclaringType with a custom callback.
        /// </summary>
        /// <param name="eventDeclaringType">Type the event is declared in</param>
        /// <param name="eventName">Name of the event</param>
        /// <param name="targetDeclaringType">Type the method to remove is declared in</param>
        /// <param name="targetName">Name of the method to remove</param>
        public ReflectedEventReplaceAttribute(Type eventDeclaringType, string eventName, Type targetDeclaringType,
            string targetName)
        {
            EventDeclaringType = eventDeclaringType;
            EventName = eventName;
            TargetDeclaringType = targetDeclaringType;
            TargetName = targetName;
        }
        
        public ReflectedEventReplaceAttribute(string eventDeclaringType, string eventName, Type targetDeclaringType,
                                              string targetName)
        {
            EventDeclaringType = Type.GetType(eventDeclaringType);
            EventName = eventName;
            TargetDeclaringType = targetDeclaringType;
            TargetName = targetName;
        }
    }
}