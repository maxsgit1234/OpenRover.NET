# OpenRover.NET
C# source-code to control an Open Rover vehicle from Rover Robotics via serial interface.

### Requirements

- Windows (tested on Windows 10 64-bit)
- .NET Framework 4.7.1 and .NET SDK (to build)
- Visual Studio 2017
- USB serial port

### Summary

The only project is a Windows command-line executable that allows the user to issue commands to a rover and receive reports from the rover using the keyboard. 

Of particular interest is the RoverChannel class, which represents a client to a single physical rover machine.

### TODO

- Tread-type rovers are not compatible.
- Motion interface specifies the scaled motor values. Would be nicer to determine these values from a requested linear/angular velocity.
- Reports received from the rover require interpretation and scaling by factors that are not documented anywhere.