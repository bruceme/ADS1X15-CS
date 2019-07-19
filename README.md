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

var singleValue = adcDevice.ReadChannel(0);

var deltaValue = adcDevice.ReadDifferential_2_3(0);

adcDevice.StartComparitor(1);

for(int i = 0; i < 10; i++)
  Console.WriteLine($"Polling - {adcDevice.PollReadComparitorSigned()}");
```
