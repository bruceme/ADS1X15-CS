# ADS1X15-CS
A C# version of an ADS1015/ADS1115 ADC converter

# Dependency

[Unosquare.RaspberryIO](https://github.com/unosquare/raspberryio)

# Setup

Install the Unosquare Raspberry package with this command from Visual Studio
``` 
PM> Install-Package Unosquare.Raspberry.IO
```

Copy / Paste this code into any .Net (4.x, Core, Std) and compile


# Usage
```
var adcDevice = new ADS1025();

// Single absolute value
Console.WriteLine($"Single - {adcDevice.ReadChannel(0)});

// Differential 2 & 3
Console.WriteLine($"Differntial 2 & 3 - {adcDevice.ReadDifferential_2_3()});

// Begin the comparitor, with 200 as the threashold
adcDevice.StartComparitor(1, 200);

// Poll the comparitor to read the values above the set threashold
for(int i = 0; i < 10; i++)
  Console.WriteLine($"Polling 1 - {adcDevice.PollReadComparitorSigned()}");
```
