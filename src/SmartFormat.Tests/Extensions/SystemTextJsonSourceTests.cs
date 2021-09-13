﻿using System;
using System.Globalization;
using System.Text.Json;
using NUnit.Framework;
using SmartFormat.Core.Extensions;
using SmartFormat.Core.Formatting;
using SmartFormat.Core.Settings;
using SmartFormat.Extensions;

namespace SmartFormat.Tests.Extensions
{
    [TestFixture]
    public class SystemTextJsonSourceTests
    {
        private const string JsonOneLevel = @"{'Name': 'Doe'}";
        private const string JsonTwoLevel = @"{'Name': {'First': 'Joe'}}";
        private const string JsonNull = @"{'Name': null}";
        private const string JsonComplex = @"{
  'Stores': [
    'Lambton Quay',
    'Willis Street'
  ],
  'Manufacturers': [
    {
      'Name': 'Acme Co',
      'Products': [
        {
          'Name': 'Anvil',
          'Price': 50,
          'OnStock': true
        }
      ]
    },
    {
      'Name': 'Contoso',
      'Products': [
        {
          'Name': 'Elbow Grease',
          'Price': 99.95,
          'OnStock': true
        },
        {
          'Name': 'Headlight Fluid',
          'Price': 4,
          'OnStock': false
        }
      ]
    }
  ]
}";
        private static SmartFormatter GetFormatterWithJsonSource(SmartSettings? settings = null)
        {
            var smart = new SmartFormatter(settings ?? new SmartSettings());
            // SystemTextJsonSource MUST be registered before ReflectionSource (which is not required here)
            // We also need the ListFormatter to process arrays
            smart.AddExtensions(new ListFormatter(), new DefaultSource(), new SystemTextJsonSource());
            smart.AddExtensions(new ListFormatter(), new NullFormatter(), new DefaultFormatter());
            return smart;
        }

        [Test]
        public void Format_Null_Json()
        {
            var jObject = JsonDocument.Parse(JsonNull.Replace("'", "\"")).RootElement;
            var result = GetFormatterWithJsonSource().Format("{Name:isnull:Value is Null|{}}", jObject);
            Assert.AreEqual("Value is Null", result);
        }

        [Test]
        public void Format_OneLevel_Json()
        {
            var jObject = JsonDocument.Parse(JsonOneLevel.Replace("'", "\"")).RootElement;
            var result = GetFormatterWithJsonSource().Format("{Name}", jObject);
            Assert.AreEqual("Doe", result);
        }

        [Test]
        public void Format_TwoLevel_Json()
        {
            var jObject = JsonDocument.Parse(JsonTwoLevel.Replace("'", "\"")).RootElement;
            var result = GetFormatterWithJsonSource().Format("{Name.First}", jObject);
            Assert.AreEqual("Joe", result);
        }

        [Test]
        public void Format_TwoLevel_Nullable_Json()
        {
            var jObject = JsonDocument.Parse(JsonNull.Replace("'", "\"")).RootElement;
            var result = GetFormatterWithJsonSource().Format("{Name?.First}", jObject);
            Assert.AreEqual("", result);
        }

        [Test]
        public void Format_Complex_Json()
        {
            var jObject = JsonDocument.Parse(JsonComplex.Replace("'", "\"")).RootElement;
            Assert.Multiple(() =>
            {
                var smart = GetFormatterWithJsonSource(new SmartSettings {CaseSensitivity = CaseSensitivityType.CaseSensitive});
                Assert.AreEqual("50.00", smart.Format(CultureInfo.InvariantCulture, "{Manufacturers[0].Products[0].Price:0.00}", jObject));
                Assert.AreEqual("True", smart.Format(CultureInfo.InvariantCulture, "{Manufacturers[1].Products[0].OnStock}", jObject));
                Assert.AreEqual("False", smart.Format(CultureInfo.InvariantCulture, "{Manufacturers[1].Products[1].OnStock}", jObject));
            });
        }

        [Test]
        public void Format_Complex_Json_CaseInsensitive()
        {
            var jObject = JsonDocument.Parse(JsonComplex.Replace("'", "\"")).RootElement;
            var smart = GetFormatterWithJsonSource(new SmartSettings {CaseSensitivity = CaseSensitivityType.CaseInsensitive});
            var result = smart.Format(CultureInfo.InvariantCulture, "{MaNuFaCtUrErS[0].PrOdUcTs[0].PrIcE:0.00}", jObject);
            Assert.AreEqual("50.00", result);
        }

        [Test]
        public void Format_LiJson()
        {
            var jObject = JsonDocument.Parse(JsonComplex.Replace("'", "\"")).RootElement;
            var result = GetFormatterWithJsonSource().Format("{Stores:list:{}|, |, and }", jObject);
            Assert.AreEqual("Lambton Quay, and Willis Street", result);
        }

        [Test]
        public void Format_Exception_Json()
        {
            var smart = GetFormatterWithJsonSource(new SmartSettings {Formatter = new FormatterSettings {ErrorAction = FormatErrorAction.ThrowError}});
            var jObject = JsonDocument.Parse(JsonOneLevel.Replace("'", "\"")).RootElement;
            Assert.Throws<FormattingException>(() => smart.Format("{Dummy}", jObject));
        }
    }
}
