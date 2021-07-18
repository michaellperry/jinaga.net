﻿using Jinaga.Facts;
using Jinaga.Managers;
using Jinaga.Observers;
using Jinaga.Pipelines;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jinaga
{
    public class Observer<TProjection>
    {
        private readonly Pipeline pipeline;
        private readonly FactReference startReference;
        private readonly FactManager factManager;
        private readonly Observation<TProjection> observation;

        private Task? initialize;
        private CancellationTokenSource cancelInitialize = new CancellationTokenSource();

        public Task Initialized => initialize!;

        internal Observer(Pipeline pipeline, FactReference startReference, FactManager factManager, Observation<TProjection> observation)
        {
            this.pipeline = pipeline;
            this.startReference = startReference;
            this.factManager = factManager;
            this.observation = observation;
        }

        internal void Start()
        {
            var cancellationToken = cancelInitialize.Token;
            initialize = Task.Run(() => RunInitialQuery(cancellationToken), cancellationToken);
        }

        public async Task Stop()
        {
            if (initialize != null)
            {
                cancelInitialize.CancelAfter(TimeSpan.FromSeconds(2));
                await initialize;
            }
        }

        private async Task RunInitialQuery(CancellationToken cancellationToken)
        {
            var products = await factManager.Query(startReference, pipeline.InitialTag, pipeline.Paths, cancellationToken);
            var results = await factManager.ComputeProjections<TProjection>(pipeline.Projection, products, cancellationToken);
            await observation.NotifyAdded(results);
        }
    }
}