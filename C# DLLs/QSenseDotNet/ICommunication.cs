using System;

namespace QSenseDotNet
{
    /// <summary>
    /// Defines an abstraction for the communication layer.
    /// </summary>
    public interface ICommunication
    {
        /// <summary>
        /// Occurs when data is received from the communication channel.
        /// </summary>
        event DataReceivedEventHandler? DataReceived;

        /// <summary>
        /// Sends the specified data through the communication channel.
        /// </summary>
        /// <param name="data">The data to send.</param>
        void Write(string data);
    }

    /// <summary>
    /// Represents the method that will handle the DataReceived event of a communication interface.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="DataReceivedEventArgs"/> that contains the event data.</param>
    public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);

    /// <summary>
    /// Provides data for the <see cref="ICommunication.DataReceived"/> event.
    /// </summary>
    public class DataReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataReceivedEventArgs"/> class with the received data.
        /// </summary>
        /// <param name="data">The data that was received.</param>
        public DataReceivedEventArgs(string data)
        {
            Data = data;
        }

        /// <summary>
        /// Gets or sets the data received from the communication channel.
        /// </summary>
        public string Data { get; set; } = "";
    }
}