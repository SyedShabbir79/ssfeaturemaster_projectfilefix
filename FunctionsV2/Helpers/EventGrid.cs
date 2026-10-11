using Azure;
using Azure.Messaging.EventGrid;
using FunctionsV2.Helpers.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace FunctionsV2.Helpers
{
    public interface IEventGrid
    {
        Task<Response> PublishToEventGrid(EGRequest request);
    }

    public class EventGrid : IEventGrid
    {
        private readonly IConfiguration _config;
        private readonly EventGridPublisherClient _eventGridClient;
        public EventGrid(IConfiguration config)
        {
            _config = config;
            _eventGridClient = new EventGridPublisherClient(
                new Uri(_config["eventGridURL"]),
                new AzureKeyCredential(_config["eventGridAccessKey"]));
        }

        public async Task<Response> PublishToEventGrid(EGRequest request)
        {

            var eventGridEvent = new EventGridEvent(
                subject: request.Subject,
                eventType: request.EventType,
                dataVersion: "1.0",
                data: request.Data
            );
            eventGridEvent.Id = request.Id;
            eventGridEvent.Topic = request.Topic;

            Response egResponse = await _eventGridClient.SendEventAsync(eventGridEvent).ConfigureAwait(false);

            return egResponse;
        }

    }
}
