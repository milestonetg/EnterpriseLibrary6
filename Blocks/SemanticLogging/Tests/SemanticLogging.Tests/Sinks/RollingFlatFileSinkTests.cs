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

using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Sinks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Tests.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Tests.Sinks
{
    [TestClass]
    public class RollingFlatFileSinkTests
    {
        string fileNameWithoutExtension;
        string fileName;
        const string extension = ".log";

        private MockDateTimeProvider dateTimeProvider;

        [TestInitialize]
        public void SetUp()
        {
            dateTimeProvider = new MockDateTimeProvider();
            AppDomain.CurrentDomain.SetData("APPBASE", Environment.CurrentDirectory);
            fileNameWithoutExtension = Guid.NewGuid().ToString();
            fileName = fileNameWithoutExtension + extension;
        }

        [TestCleanup]
        public void TearDown()
        {
            foreach (string createdFileName in Directory.GetFiles(".", fileNameWithoutExtension + "*"))
            {
                File.Delete(createdFileName);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ThrowOnNullFileName()
        {
            new RollingFlatFileSink(null, 0, string.Empty, RollFileExistsBehavior.Increment, RollInterval.Day, 0, false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ThrowOnEmptyFileName()
        {
            new RollingFlatFileSink(string.Empty, 0, string.Empty, RollFileExistsBehavior.Increment, RollInterval.Day, 0, false);
        }

        [TestMethod]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void ThrowOnDirectoryNotFound()
        {
            new RollingFlatFileSink(@"Z:\Foo\foo.log", 0, string.Empty, RollFileExistsBehavior.Increment, RollInterval.Day, 0, false);
        }

        [TestMethod]
        public void ThrowOnInvalidFileChars()
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                AssertEx.Throws<ArgumentException>(() => new RollingFlatFileSink(c.ToString(), 0, string.Empty, RollFileExistsBehavior.Increment, RollInterval.Day, 0, false));
            }

            foreach (var c in Path.GetInvalidPathChars())
            {
                AssertEx.Throws<ArgumentException>(() => new RollingFlatFileSink(c.ToString(), 0, string.Empty, RollFileExistsBehavior.Increment, RollInterval.Day, 0, false));
            }
        }

        [TestMethod]
        public void ThrowIfTimestampPatternIsNullOrEmpty()
        {
            AssertEx.Throws<ArgumentNullException>(() => new RollingFlatFileSink("rolling.log", 1024, null, RollFileExistsBehavior.Increment, RollInterval.Day, 0, false));

            AssertEx.Throws<ArgumentException>(() => new RollingFlatFileSink("rolling.log", 1024, "", RollFileExistsBehavior.Increment, RollInterval.Day, 0, false));
        }

        [TestMethod]
        public void ThrowOnPathNavigationFileName()
        {
            AssertEx.Throws<ArgumentException>(() => new RollingFlatFileSink(".", 0, string.Empty, RollFileExistsBehavior.Increment, RollInterval.Day, 0, false));
            AssertEx.Throws<ArgumentException>(() => new RollingFlatFileSink(@"..\", 0, string.Empty, RollFileExistsBehavior.Increment, RollInterval.Day, 0, false));
            AssertEx.Throws<ArgumentException>(() => new RollingFlatFileSink(@"..\..\", 0, string.Empty, RollFileExistsBehavior.Increment, RollInterval.Day, 0, false));
            AssertEx.Throws<ArgumentException>(() => new RollingFlatFileSink(@"C:\Test\..\", 0, string.Empty, RollFileExistsBehavior.Increment, RollInterval.Day, 0, false));
        }

        [TestMethod]
        public void ThrowsArgumentOutOfRangeIfTimeStampUsesInvalidChars()
        {
            AssertEx.Throws<ArgumentException>(() => new RollingFlatFileSink("rolling.log", 0, "MM/dd/yyyy", RollFileExistsBehavior.Increment, RollInterval.Day, 0, false));
            AssertEx.Throws<ArgumentException>(() => new RollingFlatFileSink("rolling.log", 0, "MM:dd:yyyy", RollFileExistsBehavior.Increment, RollInterval.Day, 0, false));
        }

        [TestMethod]
        public void SinkForNewFileWillUseCreationDateToCalculateRollDate()
        {
            using (var sink
                = new RollingFlatFileSink(fileName, 0, "yyyy", RollFileExistsBehavior.Overwrite, RollInterval.Day, 0, false))
            {
                sink.RollingHelper.DateTimeProvider = this.dateTimeProvider;

                Assert.IsTrue(sink.RollingHelper.UpdateRollingInformationIfNecessary());

                Assert.AreEqual(sink.RollingHelper.CalculateNextRollDate(File.GetCreationTime(fileName)), sink.RollingHelper.NextRollDateTime);
                Assert.IsNull(sink.RollingHelper.CheckIsRollNecessary());
            }
        }

        [TestMethod]
        public void SinkForExistingFileWillUseCreationDateToCalculateRollDate()
        {
            File.WriteAllText(fileName, "existing text");
            File.SetCreationTime(fileName, new DateTime(2000, 01, 01));

            using (var sink
                = new RollingFlatFileSink(fileName, 0, "yyyy", RollFileExistsBehavior.Overwrite, RollInterval.Day, 0, false))
            {
                this.dateTimeProvider.currentDateTime = new DateTime(2008, 01, 01);
                sink.RollingHelper.DateTimeProvider = this.dateTimeProvider;

                Assert.IsTrue(sink.RollingHelper.UpdateRollingInformationIfNecessary());

                Assert.AreEqual(sink.RollingHelper.CalculateNextRollDate(File.GetCreationTime(fileName)), sink.RollingHelper.NextRollDateTime);
                Assert.AreEqual(this.dateTimeProvider.CurrentDateTime, sink.RollingHelper.CheckIsRollNecessary());
            }
        }

        [TestMethod]
        public void WriterKeepsTally()
        {
            using (var sink
                = new RollingFlatFileSink(fileName, 10, "yyyy", RollFileExistsBehavior.Overwrite, RollInterval.Day, 0, false))
            {
                sink.RollingHelper.DateTimeProvider = this.dateTimeProvider;

                Assert.IsTrue(sink.RollingHelper.UpdateRollingInformationIfNecessary());

                sink.OnNext("12345");

                Assert.AreEqual(5L, sink.Tally);
            }
        }

        [TestMethod]
        public void RolledFileWillHaveCurrentDateForTimestamp()
        {
            using (var sink
                = new RollingFlatFileSink(fileName, 10, "yyyy", RollFileExistsBehavior.Overwrite, RollInterval.Day, 0, false))
            {
                sink.RollingHelper.DateTimeProvider = dateTimeProvider;

                sink.OnNext("1234567890");

                Assert.IsTrue(sink.RollingHelper.UpdateRollingInformationIfNecessary());

                sink.RollingHelper.PerformRoll(new DateTime(2007, 01, 01));

                sink.OnNext("12345");

                Assert.AreEqual(5L, sink.Tally);
            }

            Assert.IsTrue(File.Exists(fileName));
            Assert.AreEqual("12345", File.ReadAllText(fileName));
            Assert.IsTrue(File.Exists(fileNameWithoutExtension + ".2007" + extension));
            Assert.AreEqual("1234567890", File.ReadAllText(fileNameWithoutExtension + ".2007" + extension));
        }

        ////[TestMethod]
        ////public void FallbackFileNameIsUsedForRoll()
        ////{
        ////    using (FileStream fileStream = File.Open(fileName, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        ////    {
        ////        using (var sink
        ////            = new RollingFlatFileSink(fileName, 10, "yyyy", RollFileExistsBehavior.Overwrite, RollInterval.Day, 0, false))
        ////        {
        ////            sink.RollingHelper.DateTimeProvider = dateTimeProvider;
        ////            sink.OnNext("1234567890");

        ////            Assert.IsTrue(sink.RollingHelper.UpdateRollingInformationIfNecessary());

        ////            sink.RollingHelper.PerformRoll(new DateTime(2007, 01, 01));
        ////            sink.OnNext("12345");

        ////            Assert.AreEqual(5L, ((RollingFlatFileSink.TallyKeepingFileStreamWriter)sink.Writer).Tally);
        ////        }
        ////    }
        ////}

        [TestMethod]
        public void RolledFileWithOverwriteWillOverwriteArchiveFileIfDateTemplateMatches()
        {
            using (var sink
                = new RollingFlatFileSink(fileName, 0, "yyyy", RollFileExistsBehavior.Overwrite, RollInterval.Day, 0, false))
            {
                sink.RollingHelper.DateTimeProvider = dateTimeProvider;
                sink.OnNext("1234567890");

                Assert.IsTrue(sink.RollingHelper.UpdateRollingInformationIfNecessary());

                sink.RollingHelper.PerformRoll(new DateTime(2007, 01, 01));
                sink.OnNext("12345");

                Assert.IsTrue(sink.RollingHelper.UpdateRollingInformationIfNecessary());

                sink.RollingHelper.PerformRoll(new DateTime(2007, 01, 01));
                sink.OnNext("abcde");
            }

            Assert.IsTrue(File.Exists(fileName));
            Assert.AreEqual("abcde", File.ReadAllText(fileName));
            Assert.IsTrue(File.Exists(fileNameWithoutExtension + ".2007" + extension));
            Assert.AreEqual("12345", File.ReadAllText(fileNameWithoutExtension + ".2007" + extension));

            string[] archiveFiles = Directory.GetFiles(".", fileNameWithoutExtension + ".2007" + extension + "*");
            Assert.AreEqual(1, archiveFiles.Length);
        }

        [TestMethod]
        public void RolledFileWithOverwriteWillCreateArchiveFileIfDateTemplateDoesNotMatch()
        {
            using (var sink
                = new RollingFlatFileSink(fileName, 0, "yyyy", RollFileExistsBehavior.Overwrite, RollInterval.Day, 0, false))
            {
                sink.RollingHelper.DateTimeProvider = dateTimeProvider;
                sink.OnNext("1234567890");

                Assert.IsTrue(sink.RollingHelper.UpdateRollingInformationIfNecessary());

                sink.RollingHelper.PerformRoll(new DateTime(2007, 01, 01));
                sink.OnNext("12345");

                Assert.IsTrue(sink.RollingHelper.UpdateRollingInformationIfNecessary());

                sink.RollingHelper.PerformRoll(new DateTime(2008, 01, 01));
                sink.OnNext("abcde");
            }

            Assert.IsTrue(File.Exists(fileName));
            Assert.AreEqual("abcde", File.ReadAllText(fileName));
            Assert.IsTrue(File.Exists(fileNameWithoutExtension + ".2008" + extension));
            Assert.AreEqual("12345", File.ReadAllText(fileNameWithoutExtension + ".2008" + extension));
            Assert.IsTrue(File.Exists(fileNameWithoutExtension + ".2007" + extension));
            Assert.AreEqual("1234567890", File.ReadAllText(fileNameWithoutExtension + ".2007" + extension));

            string[] archiveFiles = Directory.GetFiles(".", fileNameWithoutExtension + ".2007" + extension + "*");
            Assert.AreEqual(1, archiveFiles.Length);
            archiveFiles = Directory.GetFiles(".", fileNameWithoutExtension + ".2008" + extension + "*");
            Assert.AreEqual(1, archiveFiles.Length);
        }

        [TestMethod]
        public void RolledFileWithOverwriteAndCapWillCreateArchiveFileIfDateTemplateDoesNotMatchAndKeepTheNewest()
        {
            using (var sink
                = new RollingFlatFileSink(
                    fileName,
                    0,
                    "yyyy",
                    RollFileExistsBehavior.Overwrite,
                    RollInterval.Day,
                    1,
                    false))
            {
                var testTime = new DateTime(2007, 01, 01);

                sink.RollingHelper.DateTimeProvider = dateTimeProvider;
                dateTimeProvider.currentDateTime = testTime;

                sink.OnNext("1234567890");

                sink.RollingHelper.PerformRoll(testTime);

                sink.OnNext("12345");

                dateTimeProvider.currentDateTime = testTime.AddYears(1);
                sink.RollingHelper.PerformRoll(testTime.AddYears(1));

                sink.OnNext("abcde");

                dateTimeProvider.currentDateTime = testTime.AddYears(2);
                sink.RollingHelper.PerformRoll(testTime.AddYears(2));

                sink.OnNext("edcbe");
            }

            Assert.IsTrue(File.Exists(fileName));
            Assert.IsTrue(File.ReadAllText(fileName).Contains("edcbe"));
            Assert.IsTrue(File.Exists(fileNameWithoutExtension + ".2009" + extension));
            Assert.IsTrue(File.ReadAllText(fileNameWithoutExtension + ".2009" + extension).Contains("abcde"));
            Assert.IsFalse(File.Exists(fileNameWithoutExtension + ".2008" + extension));
            Assert.IsFalse(File.Exists(fileNameWithoutExtension + ".2007" + extension));
        }

        [TestMethod]
        public void RolledFileWithOverwriteWillFallBackToUniqueNameIfDateTemplateMatchesButArchiveFileIsInUse()
        {
            string targetArchiveFile = fileNameWithoutExtension + ".2007" + extension;

            using (FileStream stream = File.Open(targetArchiveFile, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            {
                using (var sink
                    = new RollingFlatFileSink(fileName, 0, "yyyy", RollFileExistsBehavior.Overwrite, RollInterval.Day, 0, false))
                {
                    sink.RollingHelper.DateTimeProvider = dateTimeProvider;
                    sink.OnNext("1234567890");

                    Assert.IsTrue(sink.RollingHelper.UpdateRollingInformationIfNecessary());

                    sink.RollingHelper.PerformRoll(new DateTime(2007, 01, 01));
                    sink.OnNext("12345");
                }
            }

            Assert.IsTrue(File.Exists(fileName));
            Assert.AreEqual("12345", File.ReadAllText(fileName));
            Assert.IsTrue(File.Exists(targetArchiveFile));
            Assert.AreEqual("", File.ReadAllText(targetArchiveFile)); // couldn't archive

            string[] archiveFiles = Directory.GetFiles(".", targetArchiveFile + "*");
            Assert.AreEqual(2, archiveFiles.Length);
            foreach (string archiveFile in archiveFiles)
            {
                if (!Path.GetFileName(archiveFile).Equals(targetArchiveFile))
                {
                    Assert.AreEqual("1234567890", File.ReadAllText(archiveFile));
                }
            }
        }

        [TestMethod]
        public void RolledFileWithIncrementWillCreateArchiveFileIfDateTemplateDoesNotMatch()
        {
            using (var sink
                = new RollingFlatFileSink(fileName, 0, "yyyy", RollFileExistsBehavior.Increment, RollInterval.Day, 0, false))
            {
                sink.RollingHelper.DateTimeProvider = dateTimeProvider;
                sink.OnNext("1234567890");

                Assert.IsTrue(sink.RollingHelper.UpdateRollingInformationIfNecessary());

                sink.RollingHelper.PerformRoll(new DateTime(2007, 01, 01));
                sink.OnNext("12345");

                Assert.IsTrue(sink.RollingHelper.UpdateRollingInformationIfNecessary());

                sink.RollingHelper.PerformRoll(new DateTime(2008, 01, 01));
                sink.OnNext("abcde");
            }

            Assert.IsTrue(File.Exists(fileName));
            Assert.AreEqual("abcde", File.ReadAllText(fileName));
            Assert.IsTrue(File.Exists(fileNameWithoutExtension + ".2008.1" + extension));
            Assert.AreEqual("12345", File.ReadAllText(fileNameWithoutExtension + ".2008.1" + extension));
            Assert.IsTrue(File.Exists(fileNameWithoutExtension + ".2007.1" + extension));
            Assert.AreEqual("1234567890", File.ReadAllText(fileNameWithoutExtension + ".2007.1" + extension));

            string[] archiveFiles = Directory.GetFiles(".", fileNameWithoutExtension + ".2007*" + extension + "*");
            Assert.AreEqual(1, archiveFiles.Length);
            archiveFiles = Directory.GetFiles(".", fileNameWithoutExtension + ".2008*" + extension + "*");
            Assert.AreEqual(1, archiveFiles.Length);
        }

        [TestMethod]
        public void RolledFileWithIncrementWillCreateArchiveFileWithMaxSequenceIfDateTemplateDoesMatch()
        {
            using (var sink
                = new RollingFlatFileSink(fileName, 0, "yyyy", RollFileExistsBehavior.Increment, RollInterval.Day, 0, false))
            {
                sink.RollingHelper.DateTimeProvider = dateTimeProvider;
                sink.OnNext("1234567890");

                Assert.IsTrue(sink.RollingHelper.UpdateRollingInformationIfNecessary());

                sink.RollingHelper.PerformRoll(new DateTime(2007, 01, 01));
                sink.OnNext("12345");

                Assert.IsTrue(sink.RollingHelper.UpdateRollingInformationIfNecessary());

                sink.RollingHelper.PerformRoll(new DateTime(2007, 01, 02));
                sink.OnNext("abcde");
            }

            Assert.IsTrue(File.Exists(fileName));
            Assert.AreEqual("abcde", File.ReadAllText(fileName));
            Assert.IsTrue(File.Exists(fileNameWithoutExtension + ".2007.2" + extension));
            Assert.AreEqual("12345", File.ReadAllText(fileNameWithoutExtension + ".2007.2" + extension));
            Assert.IsTrue(File.Exists(fileNameWithoutExtension + ".2007.1" + extension));
            Assert.AreEqual("1234567890", File.ReadAllText(fileNameWithoutExtension + ".2007.1" + extension));

            string[] archiveFiles = Directory.GetFiles(".", fileNameWithoutExtension + ".2007*" + extension + "*");
            Assert.AreEqual(2, archiveFiles.Length);
        }

        [TestMethod]
        public void RolledFileWithIncrementAndCapWillCreateArchiveFileWithMaxSequenceIfDateTemplateDoesMatchAndKeepTheNewest()
        {
            using (var sink
                = new RollingFlatFileSink(
                    fileName,
                    0,
                    "yyyy",
                    RollFileExistsBehavior.Increment,
                    RollInterval.Day,
                    1,
                    false))
            {
                var testTime = new DateTime(2007, 01, 01);

                sink.RollingHelper.DateTimeProvider = dateTimeProvider;
                dateTimeProvider.currentDateTime = testTime;

                sink.OnNext("1234567890");

                dateTimeProvider.currentDateTime = testTime.AddHours(12);

                sink.RollingHelper.PerformRoll(testTime.AddHours(12));
                sink.OnNext("12345");
                dateTimeProvider.currentDateTime = testTime.AddHours(24);

                sink.RollingHelper.PerformRoll(testTime.AddHours(24));
                sink.OnNext("abcde");
            }

            Assert.IsTrue(File.Exists(fileName));
            Assert.AreEqual("abcde", File.ReadAllText(fileName));
            Assert.IsTrue(File.Exists(fileNameWithoutExtension + ".2007.2" + extension));
            Assert.AreEqual("12345", File.ReadAllText(fileNameWithoutExtension + ".2007.2" + extension));
            Assert.IsFalse(File.Exists(fileNameWithoutExtension + ".2007.1" + extension));

            string[] archiveFiles = Directory.GetFiles(".", fileNameWithoutExtension + ".2007*" + extension + "*");
            Assert.AreEqual(1, archiveFiles.Length);
        }

        [TestMethod]
        public void WillRollForDateIfEnabled()
        {
            using (var sink
                = new RollingFlatFileSink(fileName, 0, "yyyy", RollFileExistsBehavior.Increment, RollInterval.Day, 0, false))
            {
                sink.RollingHelper.DateTimeProvider = dateTimeProvider;

                dateTimeProvider.currentDateTime = DateTime.Now;
                sink.RollingHelper.UpdateRollingInformationIfNecessary();

                dateTimeProvider.currentDateTime = DateTime.Now.AddDays(2);
                Assert.IsNotNull(sink.RollingHelper.CheckIsRollNecessary());
            }

            using (var sink
                = new RollingFlatFileSink(fileName, 0, "yyyy", RollFileExistsBehavior.Increment, RollInterval.None, 0, false))
            {
                sink.RollingHelper.UpdateRollingInformationIfNecessary();

                sink.RollingHelper.DateTimeProvider = dateTimeProvider;

                dateTimeProvider.currentDateTime = DateTime.Now;
                sink.RollingHelper.UpdateRollingInformationIfNecessary();

                dateTimeProvider.currentDateTime = DateTime.Now.AddDays(2);
                Assert.IsNull(sink.RollingHelper.CheckIsRollNecessary());
            }
        }

        [TestMethod]
        public void WillRollForSize()
        {
            using (var sink
                = new RollingFlatFileSink(fileName, 1, "yyyy", RollFileExistsBehavior.Increment, RollInterval.Year, 0, false))
            {
                sink.RollingHelper.UpdateRollingInformationIfNecessary();

                sink.OnNext(new string('c', 1200));

                Assert.IsNotNull(sink.RollingHelper.CheckIsRollNecessary());
            }

            using (var sink
                = new RollingFlatFileSink(fileName, 0, "yyyy", RollFileExistsBehavior.Increment, RollInterval.Year, 0, false))
            {
                sink.RollingHelper.UpdateRollingInformationIfNecessary();

                sink.OnNext(new string('c', 1200));

                Assert.IsNull(sink.RollingHelper.CheckIsRollNecessary());
            }
        }

        [TestMethod]
        public void FindsLastSequenceOnFiles()
        {
            for (int i = 0; i < 15; i++)
            {
                if (i % 2 == 0 || i % 3 == 0)
                {
                    string tempfilename = fileNameWithoutExtension + "." + i + extension;
                    File.WriteAllText(tempfilename, "some text");
                }
            }

            int maxSequenceNumber
                = RollingFlatFileSink.StreamWriterRollingHelper.FindMaxSequenceNumber(".",
                                                                                               fileNameWithoutExtension,
                                                                                               extension);

            Assert.AreEqual(14, maxSequenceNumber);
        }

        [TestMethod]
        public void WillNotRollWhenTracingIfNotOverThresholds()
        {
            using (var sink
                = new RollingFlatFileSink(fileName, 0, "yyyy", RollFileExistsBehavior.Increment, RollInterval.Day, 0, false))
            {
                sink.RollingHelper.DateTimeProvider = dateTimeProvider;
            }
        }

        [TestMethod]
        public void WillRollExistingFileIfOverSizeThreshold()
        {
            string existingPayload = new string('c', 5000);
            DateTime currentDateTime = new DateTime(2007, 1, 1);
            File.WriteAllText(fileName, existingPayload);
            File.SetCreationTime(fileName, currentDateTime);

            using (var sink
                = new RollingFlatFileSink(fileName, 1, "yyyy", RollFileExistsBehavior.Overwrite, RollInterval.None, 0, false))
            {
                sink.RollingHelper.DateTimeProvider = dateTimeProvider;
                dateTimeProvider.currentDateTime = currentDateTime;

                sink.OnNext("logged message");
            }

            Assert.AreEqual(existingPayload, File.ReadAllText(fileNameWithoutExtension + ".2007" + extension));
            Assert.IsTrue(File.ReadAllText(fileName).Contains("logged message"));
        }

        [TestMethod]
        public void WillRollExistingFileIfOverSizeThresholdAndNoPatternIsSpecifiedForIncrementBehavior()
        {
            string existingPayload = new string('c', 5000);
            DateTime currentDateTime = new DateTime(2007, 1, 1);
            File.WriteAllText(fileName, existingPayload);
            File.SetCreationTime(fileName, currentDateTime);

            using (var sink
                = new RollingFlatFileSink(fileName, 1, "", RollFileExistsBehavior.Increment, RollInterval.None, 0, false))
            {
                sink.RollingHelper.DateTimeProvider = dateTimeProvider;
                dateTimeProvider.currentDateTime = currentDateTime;

                sink.OnNext("logged message");
            }

            Assert.AreEqual(existingPayload, File.ReadAllText(fileNameWithoutExtension + ".1" + extension));
            Assert.IsTrue(File.ReadAllText(fileName).Contains("logged message"));
        }

        [TestMethod]
        public void WillRollExistingFileIfOverSizeThresholdAndNoPatternIsSpecifiedForIncrementBehaviorWhenUsingAsync()
        {
            string existingPayload = new string('c', 5000);
            DateTime currentDateTime = new DateTime(2007, 1, 1);
            File.WriteAllText(fileName, existingPayload);
            File.SetCreationTime(fileName, currentDateTime);

            using (var sink
                = new RollingFlatFileSink(fileName, 1, "", RollFileExistsBehavior.Increment, RollInterval.None, 0, isAsync: true))
            {
                sink.RollingHelper.DateTimeProvider = dateTimeProvider;
                dateTimeProvider.currentDateTime = currentDateTime;

                sink.OnNext("logged message");
                sink.FlushAsync().Wait();
            }

            Assert.AreEqual(existingPayload, File.ReadAllText(fileNameWithoutExtension + ".1" + extension));
            Assert.IsTrue(File.ReadAllText(fileName).Contains("logged message"));
        }

        [TestMethod]
        public void WillTruncateExistingFileIfOverSizeThresholdAndNoPatternIsSpecifiedForOverwriteBehavior()
        {
            string existingPayload = new string('c', 5000);
            DateTime currentDateTime = new DateTime(2007, 1, 1);
            File.WriteAllText(fileName, existingPayload);
            File.SetCreationTime(fileName, currentDateTime);

            using (var sink
                = new RollingFlatFileSink(fileName, 1, "", RollFileExistsBehavior.Overwrite, RollInterval.None, 0, false))
            {
                sink.RollingHelper.DateTimeProvider = dateTimeProvider;
                dateTimeProvider.currentDateTime = currentDateTime;

                sink.OnNext("logged message");
            }

            Assert.IsFalse(File.ReadAllText(fileName).Contains(existingPayload));
            Assert.IsTrue(File.ReadAllText(fileName).Contains("logged message"));
        }

        [TestMethod]
        public void WillRollExistingFileIfOverDateThreshold()
        {
            string existingPayload = new string('c', 10);
            DateTime currentDateTime = new DateTime(2007, 1, 1);
            File.WriteAllText(fileName, existingPayload);
            File.SetCreationTime(fileName, currentDateTime);

            using (var sink
                = new RollingFlatFileSink(fileName, 1, "yyyy", RollFileExistsBehavior.Overwrite, RollInterval.Day, 0, false))
            {
                sink.RollingHelper.DateTimeProvider = dateTimeProvider;
                dateTimeProvider.currentDateTime = currentDateTime.AddDays(2);

                sink.OnNext("logged message");
            }

            Assert.AreEqual(existingPayload, File.ReadAllText(fileNameWithoutExtension + ".2007" + extension));
            Assert.IsTrue(File.ReadAllText(fileName).Contains("logged message"));
        }

        [TestMethod]
        public void RolledAtMidnight()
        {
            DateTime rollDate = DateTime.Now.AddDays(1).Date;

            using (var sink
                = new RollingFlatFileSink(fileName, 0, "yyyy", RollFileExistsBehavior.Increment, RollInterval.Midnight, 0, false))
            {
                sink.RollingHelper.DateTimeProvider = dateTimeProvider;

                dateTimeProvider.currentDateTime = rollDate;

                sink.RollingHelper.UpdateRollingInformationIfNecessary();

                Assert.IsNotNull(sink.RollingHelper.NextRollDateTime);
                Assert.IsNotNull(sink.RollingHelper.CheckIsRollNecessary());
                Assert.AreEqual(rollDate, sink.RollingHelper.NextRollDateTime);
            }
        }

        [TestMethod]
        public void ConcurrentAppendsEntriesToRollingFlatFile()
        {
            const int NumberOfEntries = 300;

            using (var sink = new RollingFlatFileSink(fileName, 0, "yyyy", RollFileExistsBehavior.Increment, RollInterval.Day, 0, false))
            {
                Parallel.For(0, NumberOfEntries, i => sink.OnNext("Info-" + i + ":"));
            }

            Assert.AreEqual<int>(NumberOfEntries, File.ReadAllText(fileName).Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries).Length);
        }

        [TestMethod]
        public void ConcurrentAppendsEntriesToFlatFileWhenUsingAsync()
        {
            const int NumberOfEntries = 300;

            using (var sink = new RollingFlatFileSink(fileName, 0, "yyyy", RollFileExistsBehavior.Increment, RollInterval.Day, 0, isAsync: true))
            {
                Parallel.For(0, NumberOfEntries, i => sink.OnNext("|" + i));
                sink.FlushAsync().Wait();
            }

            var entries = File.ReadAllText(fileName).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.AreEqual<int>(NumberOfEntries, entries.Length);
            for (int i = 0; i < NumberOfEntries; i++)
            {
                CollectionAssert.Contains(entries, i.ToString());
            }
        }

        [TestMethod]
        public void RollingFlatFileCreatesSubDirectories()
        {
            string file = @"dir1\dir2\rolling\patterns\practices\log.xt";
            using (var sink = new RollingFlatFileSink(file, 1024, "yyyy", RollFileExistsBehavior.Increment, RollInterval.Day, -1, false))
            {
                Assert.IsTrue(new DirectoryInfo(Path.GetDirectoryName(file)).Exists);
            }

            Directory.Delete(Path.GetDirectoryName(file), true);
        }

        class MockDateTimeProvider : RollingFlatFileSink.DateTimeProvider
        {
            public DateTime? currentDateTime = null;

            public override DateTime CurrentDateTime
            {
                get
                {
                    if (currentDateTime != null)
                        return currentDateTime.Value;

                    return base.CurrentDateTime;
                }
            }
        }
    }
}
