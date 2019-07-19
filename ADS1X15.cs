using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Abstractions;

namespace MotorTester
{
    /// <summary>
    /// Abstract base class for both ADC varients, new the specific varient to ensure it's configured correctly at construction
    /// </summary>
    public abstract class ADS1x15
    {
        public enum adsGain_t
        {
            GAIN_TWOTHIRDS = ADS1015_REG_CONFIG_PGA_6_144V,
            GAIN_ONE = ADS1015_REG_CONFIG_PGA_4_096V,
            GAIN_TWO = ADS1015_REG_CONFIG_PGA_2_048V,
            GAIN_FOUR = ADS1015_REG_CONFIG_PGA_1_024V,
            GAIN_EIGHT = ADS1015_REG_CONFIG_PGA_0_512V,
            GAIN_SIXTEEN = ADS1015_REG_CONFIG_PGA_0_256V
        }

        // Register a device on the bus
        private II2CDevice device;

        // Instance-specific properties
        protected byte i2cAddress;
        protected byte conversionDelay;
        protected byte bitShift;

        public adsGain_t Gain { get; set; }

        public ADS1x15(byte address = ADS1x15_ADDRESS)
        {
            device = Pi.I2C.AddDevice(i2cAddress = address);
            Gain = adsGain_t.GAIN_TWOTHIRDS; /* +/- 6.144V range (limited to VDD +0.3V max!) */
        }


        /**************************************************************************/
        /*!
            @brief  Gets a single-ended ADC reading from the specified channel
        */
        /**************************************************************************/
        public short ReadChannel(byte channel)
        {
            var config = SingleAndDiffConfigBase
                        | ((channel * 0x1000) + ADS1015_REG_CONFIG_MUX_SINGLE_0)
                        | ADS1015_REG_CONFIG_OS_SINGLE
                        ;

            return (channel > 3) ? (short)-1 : readADC(config);
        }

        public short ReadDifferential_0_1()
        {
            return SignBitCleanup(readADC(SingleAndDiffConfigBase | ADS1015_REG_CONFIG_MUX_DIFF_0_1));
        }

        public short ReadDifferential_2_3()
        {
            return SignBitCleanup(readADC(SingleAndDiffConfigBase | ADS1015_REG_CONFIG_MUX_DIFF_2_3));
        }

        /// <summary>
        ///  Start ADC in comparitor mode
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="threshold"></param>
        public void StartComparitor(byte channel, short threshold)
        {
            var config = ComparitorConfigBase
                        | ((channel * 0x1000) + ADS1015_REG_CONFIG_MUX_SINGLE_0)
                        ;

            // Set the high threshold register
            // Shift 12-bit results left 4 bits for the ADS1015
            writeRegister(i2cAddress, ADS1015_REG_POINTER_HITHRESH, threshold << bitShift);

            // Write config register to the ADC
            writeRegister(i2cAddress, ADS1015_REG_POINTER_CONFIG, config);
        }

        public short PollComparitorResult()
        {
            return ReadADC();
        }

        public short PollComparitorResultSigned()
        {
            return SignBitCleanup(ReadADC());
        }

        #region Man behind the curtan
        /*=========================================================================
            I2C ADDRESS/BITS
            -----------------------------------------------------------------------*/
        protected const byte ADS1x15_ADDRESS = 0x48;    // 1001 000 (ADDR = GND)
        /*=========================================================================*/

        /*=========================================================================
            CONVERSION DELAY (in mS)
            -----------------------------------------------------------------------*/
        protected const int ADS1015_CONVERSIONDELAY = 1;
        protected const int ADS1115_CONVERSIONDELAY = 8;
        /*=========================================================================*/

        /*=========================================================================
            POINTER REGISTER
            -----------------------------------------------------------------------*/
        private const byte ADS1015_REG_POINTER_MASK = 0x03;
        private const byte ADS1015_REG_POINTER_CONVERT = 0x00;
        private const byte ADS1015_REG_POINTER_CONFIG = 0x01;
        private const byte ADS1015_REG_POINTER_LOWTHRESH = 0x02;
        private const byte ADS1015_REG_POINTER_HITHRESH = 0x03;
        /*=========================================================================*/

        /*=========================================================================
            CONFIG REGISTER
            -----------------------------------------------------------------------*/
        private const ushort ADS1015_REG_CONFIG_OS_MASK = 0x8000;
        private const ushort ADS1015_REG_CONFIG_OS_SINGLE = 0x8000;  // Write: Set to start a single-conversion
        private const ushort ADS1015_REG_CONFIG_OS_BUSY = 0x0000;  // Read: Bit = 0 when conversion is in progress
        private const ushort ADS1015_REG_CONFIG_OS_NOTBUSY = 0x8000;  // Read: Bit = 1 when device is not performing a conversion

        private const ushort ADS1015_REG_CONFIG_MUX_MASK = 0x7000;
        private const ushort ADS1015_REG_CONFIG_MUX_DIFF_0_1 = 0x0000;  // Differential P = AIN0, N = AIN1 (default)
        private const ushort ADS1015_REG_CONFIG_MUX_DIFF_0_3 = 0x1000;  // Differential P = AIN0, N = AIN3
        private const ushort ADS1015_REG_CONFIG_MUX_DIFF_1_3 = 0x2000;  // Differential P = AIN1, N = AIN3
        private const ushort ADS1015_REG_CONFIG_MUX_DIFF_2_3 = 0x3000;  // Differential P = AIN2, N = AIN3
        private const ushort ADS1015_REG_CONFIG_MUX_SINGLE_0 = 0x4000;  // Single-ended AIN0
        private const ushort ADS1015_REG_CONFIG_MUX_SINGLE_1 = 0x5000;  // Single-ended AIN1
        private const ushort ADS1015_REG_CONFIG_MUX_SINGLE_2 = 0x6000;  // Single-ended AIN2
        private const ushort ADS1015_REG_CONFIG_MUX_SINGLE_3 = 0x7000;  // Single-ended AIN3

        private const ushort ADS1015_REG_CONFIG_PGA_MASK = 0x0E00;
        private const ushort ADS1015_REG_CONFIG_PGA_6_144V = 0x0000;  // +/-6.144V range = Gain 2/3
        private const ushort ADS1015_REG_CONFIG_PGA_4_096V = 0x0200;  // +/-4.096V range = Gain 1
        private const ushort ADS1015_REG_CONFIG_PGA_2_048V = 0x0400;  // +/-2.048V range = Gain 2 (default)
        private const ushort ADS1015_REG_CONFIG_PGA_1_024V = 0x0600;  // +/-1.024V range = Gain 4
        private const ushort ADS1015_REG_CONFIG_PGA_0_512V = 0x0800;  // +/-0.512V range = Gain 8
        private const ushort ADS1015_REG_CONFIG_PGA_0_256V = 0x0A00;  // +/-0.256V range = Gain 16

        private const ushort ADS1015_REG_CONFIG_MODE_MASK = 0x0100;
        private const ushort ADS1015_REG_CONFIG_MODE_CONTIN = 0x0000;  // Continuous conversion mode
        private const ushort ADS1015_REG_CONFIG_MODE_SINGLE = 0x0100;  // Power-down single-shot mode (default)

        private const ushort ADS1015_REG_CONFIG_DR_MASK = 0x00E0;
        private const ushort ADS1015_REG_CONFIG_DR_128SPS = 0x0000;  // 128 samples per second
        private const ushort ADS1015_REG_CONFIG_DR_250SPS = 0x0020;  // 250 samples per second
        private const ushort ADS1015_REG_CONFIG_DR_490SPS = 0x0040;  // 490 samples per second
        private const ushort ADS1015_REG_CONFIG_DR_920SPS = 0x0060;  // 920 samples per second
        private const ushort ADS1015_REG_CONFIG_DR_1600SPS = 0x0080;  // 1600 samples per second (default)
        private const ushort ADS1015_REG_CONFIG_DR_2400SPS = 0x00A0;  // 2400 samples per second
        private const ushort ADS1015_REG_CONFIG_DR_3300SPS = 0x00C0;  // 3300 samples per second

        private const ushort ADS1015_REG_CONFIG_CMODE_MASK = 0x0010;
        private const ushort ADS1015_REG_CONFIG_CMODE_TRAD = 0x0000;  // Traditional comparator with hysteresis (default)
        private const ushort ADS1015_REG_CONFIG_CMODE_WINDOW = 0x0010;  // Window comparator

        private const ushort ADS1015_REG_CONFIG_CPOL_MASK = 0x0008;
        private const ushort ADS1015_REG_CONFIG_CPOL_ACTVLOW = 0x0000;  // ALERT/RDY pin is low when active (default)
        private const ushort ADS1015_REG_CONFIG_CPOL_ACTVHI = 0x0008;  // ALERT/RDY pin is high when active

        private const ushort ADS1015_REG_CONFIG_CLAT_MASK = 0x0004;  // Determines if ALERT/RDY pin latches once asserted
        private const ushort ADS1015_REG_CONFIG_CLAT_NONLAT = 0x0000;  // Non-latching comparator (default)
        private const ushort ADS1015_REG_CONFIG_CLAT_LATCH = 0x0004;  // Latching comparator

        private const ushort ADS1015_REG_CONFIG_CQUE_MASK = 0x0003;
        private const ushort ADS1015_REG_CONFIG_CQUE_1CONV = 0x0000;  // Assert ALERT/RDY after one conversions
        private const ushort ADS1015_REG_CONFIG_CQUE_2CONV = 0x0001;  // Assert ALERT/RDY after two conversions
        private const ushort ADS1015_REG_CONFIG_CQUE_4CONV = 0x0002;  // Assert ALERT/RDY after four conversions
        private const ushort ADS1015_REG_CONFIG_CQUE_NONE = 0x0003;  // Disable the comparator and put ALERT/RDY in high state (default)
        /*=========================================================================*/



        const ushort SingleAndDiffConfigBase = ADS1015_REG_CONFIG_CQUE_NONE  // Disable the comparator (default val)
                                            | ADS1015_REG_CONFIG_CLAT_NONLAT  // Non-latching (default val)
                                            | ADS1015_REG_CONFIG_CPOL_ACTVLOW  // Alert/Rdy active low   (default val)
                                            | ADS1015_REG_CONFIG_CMODE_TRAD  // Traditional comparator (default val)
                                            | ADS1015_REG_CONFIG_DR_1600SPS  // 1600 samples per second (default)
                                            | ADS1015_REG_CONFIG_MODE_SINGLE    // Single-shot mode (default)
                                            ;

        const ushort ComparitorConfigBase = ADS1015_REG_CONFIG_CQUE_1CONV  // Comparator enabled and asserts on 1 match
                                        | ADS1015_REG_CONFIG_CLAT_LATCH  // Latching mode
                                        | ADS1015_REG_CONFIG_CPOL_ACTVLOW  // Alert/Rdy active low   (default val)
                                        | ADS1015_REG_CONFIG_CMODE_TRAD  // Traditional comparator (default val)
                                        | ADS1015_REG_CONFIG_DR_1600SPS  // 1600 samples per second (default)
                                        | ADS1015_REG_CONFIG_MODE_CONTIN  // Continuous conversion mode
                                        | ADS1015_REG_CONFIG_MODE_CONTIN   // Continuous conversion mode
                                        ;

        private void writeRegister(byte i2cAddress, byte reg, int value)
        {
            // Wire.beginTransmission(i2cAddress);
            device.Write(reg);
            device.Write((byte)(value >> 8));
            device.Write((byte)(value & 0xFF));
            // Wire.endTransmission();
        }

        private ushort readRegister(byte i2cAddress, byte reg)
        {
            //Wire.beginTransmission(i2cAddress);
            device.Write(ADS1015_REG_POINTER_CONVERT);
            //Wire.endTransmission();
            return device.ReadAddressWord(i2cAddress);
        }


        /// <summary>
        /// Refactored core reader
        /// </summary>
        /// <param name="chanelConfig"></param>
        /// <returns></returns>
        private short readADC(int config)
        {
            // Write config register to the ADC
            writeRegister(i2cAddress, ADS1015_REG_POINTER_CONFIG, ((int)Gain | config));

            return ReadADC();
        }

        private short ReadADC()
        {
            // Wait for the conversion to complete
            Thread.Sleep(conversionDelay);

            // Read the conversion results
            // Shift 12-bit results right 4 bits for the ADS1015
            return (short)(readRegister(i2cAddress, ADS1015_REG_POINTER_CONVERT) >> bitShift);
        }

        /// <summary>
        /// The bit shifting wrecks the sign bit, need to get it back in the right spot
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        private short SignBitCleanup(int res)
        {
            return (short)((bitShift != 0 && res > 0x07FF) ? res | 0xF000 : res);
        }
        
        #endregion
    }

    public class ADS1015 : ADS1x15
    {
        public ADS1015(byte address = ADS1x15_ADDRESS) : base(address)
        {
            conversionDelay = ADS1015_CONVERSIONDELAY;
            bitShift = 4;
        }
    }

    public class ADS1115 : ADS1x15
    {
        public ADS1115(byte address = ADS1x15_ADDRESS) : base(address)
        {
            conversionDelay = ADS1115_CONVERSIONDELAY;
            bitShift = 0;
        }
    }
}
