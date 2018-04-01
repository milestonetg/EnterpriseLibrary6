﻿#region license
// ==============================================================================
// Microsoft patterns & practices Enterprise Library
// Semantic Logging Application Block
// ==============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ==============================================================================
#endregion

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Utility;

namespace Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Sinks
{
    /// <summary>
    /// A sink that writes to a flat file.
    /// </summary>    
    /// <remarks>This class is thread-safe.</remarks>
    public class FlatFileSink : IObserver<string>, IDisposable
    {
        private readonly bool isAsync;
        private readonly object lockObject = new object();
        private readonly object flushLockObject = new object();
        private StreamWriter writer;
        private bool disposed;
        private BlockingCollection<string> pendingEntries;
        private volatile TaskCompletionSource<bool> flushSource = new TaskCompletionSource<bool>();
        private CancellationTokenSource cancellationTokenSource;
        private Task asyncProcessorTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlatFileSink" /> class.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="isAsync">Specifies if the writing should be done asynchronously, or synchronously with a blocking call.</param>
        public FlatFileSink(string fileName, bool isAsync)
        {
            var file = FileUtil.ProcessFileNameForLogging(fileName);

            this.writer = new StreamWriter(file.Open(FileMode.Append, FileAccess.Write, FileShare.Read));

            this.isAsync = isAsync;

            this.flushSource.SetResult(true);

            if (isAsync)
            {
                this.cancellationTokenSource = new CancellationTokenSource();
                this.pendingEntries = new BlockingCollection<string>();
                this.asyncProcessorTask = Task.Factory.StartNew((Action)this.WriteEntries, TaskCreationOptions.LongRunning);
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="FlatFileSink"/> class.
        /// </summary>
        ~FlatFileSink()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Flushes the buffer content to the file.
        /// </summary>
        /// <returns>The Task that gets completed when the buffer is flushed.</returns>
        public Task FlushAsync()
        {
            lock (this.flushLockObject)
            {
                return this.flushSource.Task;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">A value indicating whether or not the class is disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.disposed)
                {
                    lock (this.lockObject)
                    {
                        if (!this.disposed)
                        {
                            this.disposed = true;

                            if (this.isAsync)
                            {
                                this.cancellationTokenSource.Cancel();
                                this.asyncProcessorTask.Wait();
                                this.pendingEntries.Dispose();
                                this.cancellationTokenSource.Dispose();
                            }

                            this.writer.Dispose();
                        }
                    }
                }
            }
        }

        private void OnSingleEventWritten(string entry)
        {
            try
            {
                lock (this.lockObject)
                {
                    this.writer.Write(entry);
                    this.writer.Flush();
                }
            }
            catch (Exception e)
            {
                SemanticLoggingEventSource.Log.FlatFileSinkWriteFailed(e.ToString());
            }
        }

        private void WriteEntries()
        {
            string entry;
            var token = this.cancellationTokenSource.Token;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (this.pendingEntries.Count == 0 && !this.flushSource.Task.IsCompleted && !token.IsCancellationRequested)
                    {
                        this.writer.Flush();
                        lock (this.flushLockObject)
                        {
                            if (this.pendingEntries.Count == 0 && !this.flushSource.Task.IsCompleted && !token.IsCancellationRequested)
                            {
                                this.flushSource.TrySetResult(true);
                            }
                        }
                    }

                    entry = this.pendingEntries.Take(token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                try
                {
                    this.writer.Write(entry);
                }
                catch (Exception e)
                {
                    SemanticLoggingEventSource.Log.FlatFileSinkWriteFailed(e.ToString());
                }
            }

            lock (this.flushLockObject)
            {
                this.flushSource.TrySetResult(true);
            }
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="FlatFileSink"/> class.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// </summary>
        public void OnCompleted()
        {
            this.FlushAsync().Wait();
            this.Dispose();
        }

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public void OnError(Exception error)
        {
            this.FlushAsync().Wait();
            this.Dispose();
        }

        /// <summary>
        /// Provides the sink with new data to write.
        /// </summary>
        /// <param name="value">The current entry to write to the file.</param>
        public void OnNext(string value)
        {
            if (value != null)
            {
                if (this.isAsync)
                {
                    this.pendingEntries.Add(value);

                    if (this.flushSource.Task.IsCompleted)
                    {
                        lock (this.flushLockObject)
                        {
                            if (this.flushSource.Task.IsCompleted)
                            {
                                this.flushSource = new TaskCompletionSource<bool>();
                            }
                        }
                    }
                }
                else
                {
                    this.OnSingleEventWritten(value);
                }
            }
        }
    }
}
