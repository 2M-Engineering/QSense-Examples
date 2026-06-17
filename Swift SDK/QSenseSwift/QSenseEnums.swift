//
//  QSenseEnums.swift
//  QSenseSwift
//
//  Created by 2M Engineering ltd on 18/07/2024.
//

import Foundation
import CoreBluetooth

public enum DataModes : Int
{
    case Mixed = 0
    case Raw
    case Quat
    case Optimized
    case Quat6Dof
}

public enum LEDAnimation : Int
{
    case Blinking = 0
    case Fixed = 1
}

public enum Algorithms : Int
{
    case _9Dof = 0
    case _6Dof = 1
}

public enum MagInterference : Int
{
    case None = 1
    case SoftIron = 2
    case HardIron = 3
    case ChangeOfEnvironment = 4
}

public enum SamplingRates : Int
{
    case Hz50 = 7
    case Hz100 = 8
    case Hz200 = 9
    case Hz400 = 10
    case Hz800 = 11
}

public enum SensitivityAcc : Int
{
    case G2 = 0
    case G4 = 2
    case G8 = 3
    case G16 = 1
}

public enum SensitivityGyr : Int
{
    case Dps250 = 0
    case Dps125 = 1
    case Dps500 = 2
    case Dps1000 = 4
    case Dps2000 = 6
}

public typealias ManagerState = CBManagerState;
