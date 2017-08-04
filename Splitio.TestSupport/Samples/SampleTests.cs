﻿using Splitio.Services.Client.Classes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Extensions;

namespace Splitio.TestSupport
{
    public class SampleTest
    {
        private SplitClientForTest splitClient;

        [Theory]
        [SplitTest(test: @"{ feature:'feature_reads', treatments:['on', 'dark', 'off'] }")]
        public void SampleTest1(string feature, string treatment)
        {
            //Arrange
            splitClient = new SplitClientForTest();
            splitClient.RegisterTreatment(feature, treatment);
            
            //Act
            var actual = splitClient.GetTreatment("test", feature);

            //Assert
            Assert.Equal(treatment, actual);
        }

        [Theory]
        [SplitScenario(features: 
         @"[
            { feature:'feature_reads', treatments:['on', 'dark', 'off'] },
            { feature:'feature_reads2', treatments:['on2', 'dark2', 'off2'] }
            ]")]
        public void SampleTest2(string feature, string treatment)
        {
            //Arrange
            splitClient = new SplitClientForTest();
            splitClient.RegisterTreatment(feature, treatment);

            //Act
            var actual = splitClient.GetTreatment("test", feature);

            //Assert
            Assert.Equal(treatment, actual);
        }

        [Theory]
        [SplitSuite(scenarios:
         @"[{
               features:
               [{ feature:'feature_reads', treatments:['on', 'dark', 'off'] },
                { feature:'feature_reads2', treatments:['on2', 'dark2', 'off2'] }]
            },
            {  features:
               [{ feature:'feature_reads', treatments:['on', 'dark', 'off'] },
                { feature:'feature_reads2', treatments:['on2', 'dark2', 'off2'] }]
            }]")]
        public void SampleTest3(string feature, string treatment)
        {
            //Arrange
            splitClient = new SplitClientForTest();
            splitClient.RegisterTreatment(feature, treatment);

            //Act
            var actual = splitClient.GetTreatment("test", feature);

            //Assert
            Assert.Equal(treatment, actual);
        }
        

    }


}
