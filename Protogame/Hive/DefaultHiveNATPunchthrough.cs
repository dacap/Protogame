﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using HiveMP.NATPunchthrough.Api;
using HiveMP.NATPunchthrough.Model;
using HiveMP.TemporarySession.Model;

namespace Protogame
{
    /// <summary>
    /// Default implementation of <see cref="IHiveNATPunchthrough"/>.
    /// </summary>
    /// <module>Hive</module>
    /// <internal>true</internal>
    /// <interface_ref>Protogame.IHiveNATPunchthrough</interface_ref>
    public class DefaultHiveNATPunchthrough : IHiveNATPunchthrough
    {
        public async Task PerformNATPunchthrough(TempSessionWithSecrets userSession, UdpClient udpClient, int timeout)
        {
            await PerformNATPunchthroughInternal(userSession, udpClient, timeout);
        }

        public async Task<UdpClient> PerformNATPunchthrough(TempSessionWithSecrets userSession, int timeout)
        {
            return await PerformNATPunchthroughInternal(userSession, null, timeout);
        }

        public async Task<List<NATEndpoint>> LookupEndpoints(TempSessionWithSecrets userSession, string targetSession)
        {
            var natPunchthroughApi = new NATPunchthroughApi();
            natPunchthroughApi.Configuration.ApiKey["api_key"] = userSession.ApiKey;
            return await natPunchthroughApi.EndpointsGetAsync(targetSession);
        }

        private async Task<UdpClient> PerformNATPunchthroughInternal(TempSessionWithSecrets userSession, UdpClient specificUdpClient, int timeout)
        {
            var natPunchthroughApi = new NATPunchthroughApi();
            natPunchthroughApi.Configuration.ApiKey["api_key"] = userSession.ApiKey;

            var start = DateTime.UtcNow;

            var udpClient = specificUdpClient ?? new UdpClient();

            while (true)
            {
                var negotation = await natPunchthroughApi.PunchthroughPutAsync(userSession.Id);
                if (negotation.Port == null)
                {
                    throw new InvalidOperationException();
                }

                await udpClient.SendAsync(
                    negotation.Message,
                    negotation.Message.Length,
                    negotation.Host,
                    negotation.Port.Value);

                await Task.Delay(1000);

                if (await natPunchthroughApi.PunchthroughGetAsync(userSession.Id) == true)
                {
                    // NAT punchthrough completed successfully.
                    return udpClient;
                }

                if (timeout > 0 && (DateTime.UtcNow - start).TotalMilliseconds > timeout)
                {
                    throw new TimeoutException("Unable to perform NAT punchthrough before the timeout occurred.");
                }

                await Task.Delay(1000);
            }
        }
    }
}
