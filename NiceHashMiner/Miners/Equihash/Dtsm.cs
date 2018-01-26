﻿using Newtonsoft.Json;
using NiceHashMiner.Enums;
using NiceHashMiner.Miners.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NiceHashMiner.Miners
{
    public class Dtsm : Miner
    {
        private const double DevFee = 2.0;

        public Dtsm() : base("dtsm")
        {
            ConectionType = NhmConectionType.NONE;
        }
        protected override int GetMaxCooldownTimeInMilliseconds()
        {
            return 60 * 1000 * 5;
        }

        public override void Start(string url, string btcAdress, string worker)
        {
            LastCommandLine = GetStartCommand(url, btcAdress, worker);
            ProcessHandle = _Start();
        }

        private string GetStartCommand(string url, string btcAddress, string worker)
        {
            var urls = url.Split(':');
            var server = urls.Length > 0 ? urls[0] : "";
            var port = urls.Length > 1 ? urls[1] : "";
            return $" {GetDeviceCommand()} " +
                   $"--server {server} " +
                   $"--port {port} " +
                   $"--user {btcAddress}.{worker} " +
                   $"--telemetry=127.0.0.1:{ApiPort} ";
        }

        private string GetDeviceCommand()
        {
            var dev = MiningSetup.MiningPairs.Aggregate(" --dev ", (current, nvPair) => current + nvPair.Device.ID + " ");
            dev += ExtraLaunchParametersParser.ParseForMiningSetup(MiningSetup, DeviceType.NVIDIA);
            return dev;
        }

        protected override void _Stop(MinerStopType willswitch)
        {
            Stop_cpu_ccminer_sgminer_nheqminer(willswitch);
        }

        protected override string BenchmarkCreateCommandLine(Algorithm algorithm, int time)
        {
            throw new NotImplementedException();
        }

        protected override void BenchmarkOutputErrorDataReceivedImpl(string outdata)
        {
            throw new NotImplementedException();
        }

        protected override bool BenchmarkParseLine(string outdata)
        {
            throw new NotImplementedException();
        }

        #region API

        public override async Task<ApiData> GetSummaryAsync()
        {
            var ad = new ApiData(MiningSetup.CurrentAlgorithmType);
            var request = JsonConvert.SerializeObject(new
            {
                method = "getstat",
                id = 1
            });

            var response = await GetApiDataAsync(ApiPort, request);
            DtsmResponse resp = null;

            try
            {
                resp = JsonConvert.DeserializeObject<DtsmResponse>(response);
            }
            catch (Exception e)
            {
                Helpers.ConsolePrint(MinerTag(), e.Message);
            }

            if (resp?.result != null)
            {
                ad.Speed = resp.result.Sum(gpu => gpu.sol_ps);
            }

            return ad;
        }

        protected override bool IsApiEof(byte third, byte second, byte last)
        {
            return second == 125 && last == 10;
        }

        #region JSON Models
#pragma warning disable

        public class DtsmResponse
        {
            public List<DtsmGpuResult> result { get; set; }
        }

        public class DtsmGpuResult
        {
            public double sol_ps { get; set; } = 0;
        }

#pragma warning restore
        #endregion

        #endregion
    }
}
