namespace MassTransit.PrometheusIntegration
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Observers;


    public static class PrometheusConfigurationExtensions
    {
        /// <summary>
        /// Configure the bus to capture metrics for publication using Prometheus.
        /// </summary>
        /// <param name="configurator"></param>
        /// <param name="configureOptions"></param>
        /// <param name="serviceName">The service name for metrics reporting, defaults to the current process main module filename</param>
        public static void UsePrometheusMetrics(this IBusFactoryConfigurator configurator,
            Action<PrometheusMetricsOptions> configureOptions = null,
            string serviceName = default)
        {
            var options = PrometheusMetricsOptions.CreateDefault();

            configureOptions?.Invoke(options);

            PrometheusMetrics.TryConfigure(GetServiceName(serviceName), options);

            configurator.ConnectConsumerConfigurationObserver(new PrometheusConsumerConfigurationObserver());
            configurator.ConnectHandlerConfigurationObserver(new PrometheusHandlerConfigurationObserver());
            configurator.ConnectSagaConfigurationObserver(new PrometheusSagaConfigurationObserver());
            configurator.ConnectActivityConfigurationObserver(new PrometheusActivityConfigurationObserver());
            configurator.ConnectEndpointConfigurationObserver(new PrometheusReceiveEndpointConfiguratorObserver());
            configurator.ConnectBusObserver(new PrometheusBusObserver());
        }

        static string GetServiceName(string serviceName)
        {
            return string.IsNullOrWhiteSpace(serviceName)
                ? Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName)
                : serviceName;
        }

        static string DefaultMessageTypeParser(IEnumerable<string> supportedMessageTypes)
        {
            return ExtractMessageType(supportedMessageTypes.FirstOrDefault() ?? "unknown");

            string ExtractMessageType(string urnMessageTypeName)
            {
                if (!urnMessageTypeName.Contains(':'))
                    return urnMessageTypeName;
                try
                {
                    return urnMessageTypeName.Split(':').Last().Split('.').Last().Replace('+', '.');
                }
                catch (Exception)
                {
                    return urnMessageTypeName;
                }
            }
        }
    }


    public delegate string ParseMessageType(IEnumerable<string> supportedMessageTypes);
}
