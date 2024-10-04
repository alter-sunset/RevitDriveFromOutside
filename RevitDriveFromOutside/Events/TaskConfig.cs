﻿namespace RevitDriveFromOutside.Events
{
    /// <summary>
    /// Object that will hold configuration obtained from message
    /// </summary>
    public class TaskConfig
    {
        /// <summary>
        /// External Event to call
        /// </summary>
        public ExternalEvents ExternalEvent { get; set; }
        /// <summary>
        /// Object with configuration if given event needs one
        /// </summary>
        public object? EventConfig { get; set; }
    }
}