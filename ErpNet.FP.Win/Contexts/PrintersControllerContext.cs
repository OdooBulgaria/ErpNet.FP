﻿using ErpNet.FP.Core;
using ErpNet.FP.Core.Drivers.BgDaisy;
using ErpNet.FP.Core.Drivers.BgDatecs;
using ErpNet.FP.Core.Drivers.BgEltrade;
using ErpNet.FP.Core.Drivers.BgTremol;
using ErpNet.FP.Core.Provider;
using ErpNet.FP.Win.Transports;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace ErpNet.FP.Win.Contexts
{
    public interface IPrintersControllerContext
    {
        Dictionary<string, DeviceInfo> PrintersInfo { get; }

        Dictionary<string, IFiscalPrinter> Printers { get; }
    }

    public class PrintersControllerContext : IPrintersControllerContext
    {

        public Provider Provider { get; } = new Provider();
        public Dictionary<string, DeviceInfo> PrintersInfo { get; } = new Dictionary<string, DeviceInfo>();

        public Dictionary<string, IFiscalPrinter> Printers { get; } = new Dictionary<string, IFiscalPrinter>();

        public PrintersControllerContext()
        {
            bool autoDetect = true;

            IConfigurationRoot? config = null;
            try
            {
                config = new ConfigurationBuilder()
                     .AddJsonFile("config.json")
                     .Build();

                autoDetect = (config["autodetect"] != "false");
            }
            catch
            {
                // We do not have a config.json
            }

            // Transports
            var comTransport = new ComTransport();

            // Drivers
            var daisyIsl = new BgDaisyIslFiscalPrinterDriver();
            var datecsPIsl = new BgDatecsPIslFiscalPrinterDriver();
            var datecsCIsl = new BgDatecsCIslFiscalPrinterDriver();
            var datecsXIsl = new BgDatecsXIslFiscalPrinterDriver();
            var eltradeIsl = new BgEltradeIslFiscalPrinterDriver();
            var tremolZfp = new BgTremolZfpFiscalPrinterDriver();

            // Add drivers and their compatible transports to the provider.
            Provider
                .Register(daisyIsl, comTransport)
                .Register(datecsPIsl, comTransport)
                .Register(datecsCIsl, comTransport)
                .Register(datecsXIsl, comTransport)
                .Register(eltradeIsl, comTransport)
                .Register(tremolZfp, comTransport);

            if (autoDetect)
            {
                System.Console.WriteLine("Detecting available printers...");
                var printers = Provider.DetectAvailablePrinters();
                foreach (KeyValuePair<string, IFiscalPrinter> printer in printers)
                {
                    // We use serial number of local connected fiscal printers as Printer ID
                    var baseID = printer.Value.DeviceInfo.SerialNumber.ToLowerInvariant();

                    var printerID = baseID;
                    int duplicateNumber = 0;
                    while (PrintersInfo.ContainsKey(printerID))
                    {
                        duplicateNumber++;
                        printerID = $"{baseID}_{duplicateNumber}";
                    }
                    PrintersInfo.Add(printerID, printer.Value.DeviceInfo);
                    Printers.Add(printerID, printer.Value);
                    System.Console.WriteLine($"Found {printerID}: {printer.Value.DeviceInfo.Uri}");
                }
                System.Console.WriteLine($"Detecting done. Found {Printers.Count} printer(s).");
            }
            if (config != null)
            {
                /* TODO: deserialize printers
                var configuredPrinters = config["printers"];
                foreach (var configuredPrinter in configuredPrinters)
                {
                    try
                    {
                        var printer = Provider.Connect(configuredPrinter["uri"]);
                        var baseID = printer.Value.DeviceInfo.SerialNumber.ToLowerInvariant();

                        var printerID = baseID;
                        int duplicateNumber = 0;
                        while (PrintersInfo.ContainsKey(printerID))
                        {
                            duplicateNumber++;
                            printerID = $"{baseID}_{duplicateNumber}";
                        }
                        PrintersInfo.Add(printerID, printer.Value.DeviceInfo);
                        Printers.Add(printerID, printer.Value);
                    }
                    catch
                    {

                    }
                }
                */
            }
        }
    }
}
