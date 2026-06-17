using System;

namespace QSenseDotNet
{
    /// <summary>
    /// Occurs when the battery level is received.
    /// </summary>
    /// 
    public delegate void BatteryReceivedEventHandler(object sender, BatteryReceivedEventArgs e);
    /// <summary>
    /// Occurs when a stream packet is received.
    /// </summary>
    public delegate void StreamPacketReceivedEventHandler(object sender, StreamPacketReceivedEventArgs e);
    /// <summary>
    /// Occurs when the name of the device changes
    /// </summary>
    /// 
    public delegate void DeviceNameChangedEventHandler(object sender, DeviceNameChangedEventArgs e);
    /// <summary>
    /// Occurs when the energy level is received.
    /// </summary>
    /// 
    public delegate void MotionLevelReceivedEventHandler(object sender, MotionLevelReceivedEventArgs e);
    /// <summary>
    /// Occurs when the device state is received.
    /// </summary>
    /// 
    public delegate void StateReceivedEventHandler(object sender, StateReceivedEventArgs e);
}
