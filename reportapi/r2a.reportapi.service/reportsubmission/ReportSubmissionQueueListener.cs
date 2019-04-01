using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json;
using NLog;
using R2A.ReportApi.Models;
using R2A.ReportApi.Service.Infrastructure;
using R2A.ReportApi.Service.Model;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;


namespace R2A.ReportApi.Service.ReportSubmission
{
    public class ReportSubmissionQueueListener : MessageQueueListener
    {
        private readonly Settings _settings;
        private readonly IMediator _mediator;
        private readonly MessageDumpService _dumpService;
        private readonly ILogger _logger;

        public ReportSubmissionQueueListener(ConnectionFactory connectionFactory, Settings settings,
            ILogFactory logFactory, IMediator mediator, MessageDumpService dumpService)
            : base(connectionFactory, logFactory, settings.QueueExchange, settings.ReportSubmissionRoutingId, settings.ReportSubmissionQueueName)
        {
            _settings = settings;
            _mediator = mediator;
            _dumpService = dumpService;
            _logger = logFactory.GetLogger<ReportSubmissionQueueListener>();
        }


        protected override async Task OnMessageReceived(object model, BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                
                var jsonString = Encoding.UTF8.GetString(ea.Body);
                var headers = MessageHeaders.FromRabbitMqHeaders(ea.BasicProperties.Headers);
                if (!headers.ContainsKey(MessageHeaders.Token))
                {
                    _logger.Error("The message header did not contain an appropriate token header.");
                    return;
                }

                var token = new Guid(headers[MessageHeaders.Token]);

                var dumpTask = _dumpService.DumpMessage(jsonString, headers, token);
                var processTask = ProcessSubmission(JsonConvert.DeserializeObject<ReportSubmissionDto>(jsonString),headers,token, cancellationToken);

                await Task.WhenAll(dumpTask, processTask);
            }
            catch (Exception e)
            {
                _logger.Error(e,"There was an error while trying to deserialize the message body.");
            }


        }

        public async Task ProcessSubmission(ReportSubmissionDto model, IDictionary<string, string> headers, Guid token, CancellationToken cancellationToken)
        {
            try
            {
                var statusInfo = (await _mediator.Send(new ReportStatus.GetInfo.Query(token), cancellationToken)).Result;
                if (statusInfo.SubmissionStatus.StatusCode != SubmissionStatus.InQueue.Code)
                {
                    _logger.Warn(
                        $"The message corresponds to a report submission that is already (being) processed. Id: {statusInfo.Id}");
                    return;
                }

                await _mediator.Send(new ReportSubmission.ReportStatus.Update.Command(token,
                    StatusCodeDto.FromStatus(SubmissionStatus.Processing), StatusCodeDto.FromStatus(DataProcessingStatus.NotApplicable)), cancellationToken);
                await _mediator.Send(new ReportSubmissionRequest(model, statusInfo, headers), cancellationToken);
            }
            catch (Exception e)
            {
                if (e is TaskCanceledException)
                {
                    _logger.Warn($"Processing of token {token} was canceled.");
                }
                else
                {
                    _logger.Error(e, $"An error occured during the submission process of token {token}.");
                }
                try
                {
                    await _mediator.Send(
                        new ReportSubmission.ReportStatus.Update.Command(token,
                            StatusCodeDto.FromStatus(SubmissionStatus.Error), null));
                }
                catch (Exception e2)
                {
                    _logger.Error(e2,$"Additionally, an error occured trying to set the submission status to {SubmissionStatus.Error}.");
                }
            }
        }
    }
}
