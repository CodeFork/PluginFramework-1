﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Moq;
using PluginFramework.Logging;
using PluginFramework.Tests.Mocks;

namespace PluginFramework.Tests
{
  [TestClass]
  public class UnitTest_PluginCreator : MarshalByRefObject
  {
    #region GetCreator
    [TestMethod]
    public void GetCreatorCanReturnCreatorInsideCurrentAppDomain()
    {
      IPluginCreator tested = PluginCreator.GetCreator();
      Assert.IsNotNull(tested);
    }

    [TestMethod]
    public void GetCreatorCanReturnCreatorInsideAnotherAppDomain()
    {
      using (MockDomain otherDomain = new MockDomain())
      {
        IPluginCreator tested = PluginCreator.GetCreator(otherDomain);
        Assert.IsNotNull(tested);
      }
    }

    [TestMethod]
    public void GetCreatorRequiresArgument()
    {
      DoAssert.Throws<ArgumentNullException>(() => PluginCreator.GetCreator(null));
    }

    [TestMethod]
    public void GetCreatorShouldReturnSameInstanceForSameDomain()
    {
      using (MockDomain otherDomain = new MockDomain())
      {
        IPluginCreator first = PluginCreator.GetCreator(otherDomain);
        IPluginCreator tested = PluginCreator.GetCreator(otherDomain);
        Assert.AreSame(first, tested);
      }
    }

    [TestMethod]
    public void GetCreatorShouldReturnDifferentInstanceForDifferentDomain()
    {
      using (MockDomain domain1 = new MockDomain())
      using (MockDomain domain2 = new MockDomain())
      {
        IPluginCreator tested1 = PluginCreator.GetCreator(domain1);
        IPluginCreator tested2 = PluginCreator.GetCreator(domain2);
        Assert.AreNotSame(tested1, tested2);
      }
    }
    #endregion

    #region Create
    [TestMethod]
    public void CreateRequiresPluginDescriptor()
    {
      IAssemblyRepository repository = new MockAssemblyRepository();
      Dictionary<string, object> settings = new Dictionary<string,object>();
      IPluginCreator tested = PluginCreator.GetCreator();
      DoAssert.Throws<ArgumentNullException>(() => tested.Create(null, repository, settings));
    }

    [TestMethod]
    public void CreateRequiresAssemblyRepository()
    {
      PluginDescriptor descriptor = MockPluginDescriptor.For<MockPlugin1>();
      Dictionary<string, object> settings = new Dictionary<string, object>();
      IPluginCreator tested = PluginCreator.GetCreator();
      DoAssert.Throws<ArgumentNullException>(() => tested.Create(descriptor, null, settings));
    }

    [TestMethod]
    public void CreateShouldCreatePluginInstance()
    {
      IAssemblyRepository repository = new MockAssemblyRepository();
      IPluginCreator tested = PluginCreator.GetCreator();
      PluginDescriptor descriptor = MockPluginDescriptor.For<MockPlugin1>();
      object plugin = tested.Create(descriptor, repository, null);
      Assert.IsNotNull(plugin);
    }

    [TestMethod]
    public void CreateShouldResolveMissingAssemblies()
    {
      using (MockDomain domain = new MockDomain())
      {
        MockAssemblyRepository repository = new MockAssemblyRepository();
        IPluginCreator tested = PluginCreator.GetCreator(domain);
        PluginDescriptor descriptor = MockPluginDescriptor.For<MockPlugin1>();
        object instance = tested.Create(descriptor, repository, null);
        Assert.IsNotNull(instance);
        Assert.IsTrue(repository.Fetched.Keys.Contains(typeof(MockPlugin1).Assembly.FullName));
      }
    }

    [TestMethod]
    public void CreateShouldThrowOnUnresolvedAssembly()
    {
      using (MockDomain domain = new MockDomain())
      {
        MockAssemblyRepository repository = new MockAssemblyRepository();
        IPluginCreator tested = PluginCreator.GetCreator(domain);
        QualifiedName fakeName = new QualifiedName(
          typeof(string).FullName.Replace("mscorlib", "NonExistingAssemblyName"),
          typeof(string).Assembly.FullName.Replace("mscorlib", "NonExistingAssemblyName"));
        
        PluginDescriptor descriptor = MockPluginDescriptor.For(fakeName);

        PluginException ex = DoAssert.Throws<PluginException>(() => tested.Create(descriptor, repository, null));
        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType(ex.InnerException, typeof(FileNotFoundException));
      }
    } 

    [TestMethod]
    public void CreateShouldThrowIfMissingRequiredSettings()
    {
      MockAssemblyRepository repository = new MockAssemblyRepository();
      IPluginCreator tested = PluginCreator.GetCreator();
      PluginDescriptor descriptor = MockPluginDescriptor.For<MockPlugin2>();

      DoAssert.Throws<PluginSettingException>(() => tested.Create(descriptor, repository, null));        
    }

    [TestMethod]
    public void CreateShouldApplySettings()
    {
      MockAssemblyRepository repository = new MockAssemblyRepository();
      IPluginCreator tested = PluginCreator.GetCreator();
      PluginDescriptor descriptor = MockPluginDescriptor.For<MockPlugin2>();

      Dictionary<string, object> settings = new Dictionary<string, object>()
      {
        { "NamedSetting", 42}
      };

      MockPlugin2 plugin = tested.Create(descriptor, repository, settings) as MockPlugin2;
      Assert.AreEqual(42, plugin.Setting);
    }

    [TestMethod]
    public void CreateShouldThrowIfSettingIsWrongType()
    {
      MockAssemblyRepository repository = new MockAssemblyRepository();
      IPluginCreator tested = PluginCreator.GetCreator();
      PluginDescriptor descriptor = MockPluginDescriptor.For<MockPlugin2>();

      Dictionary<string, object> settings = new Dictionary<string, object>()
      {
        { "NamedSetting", "not int" }
      };

      DoAssert.Throws<PluginSettingException>(() => tested.Create(descriptor, repository, settings));
    }
    #endregion

    #region Logging
    [TestMethod]
    public void ShouldSetALogger()
    {
      IPluginCreator tested = PluginCreator.GetCreator();
      ILogWriter logwriter = tested as ILogWriter;
      Assert.IsNotNull(logwriter.Log);
    }

    [TestMethod]
    public void InternalGetCreatorRequiresDomain()
    {
      DoAssert.Throws<ArgumentNullException>(() => PluginCreator.GetCreator(null, Logger.Singleton.LoggerFactory));
    }

    [TestMethod]
    public void InternalGetCreatorRequiresILoggerFactory()
    {
      DoAssert.Throws<ArgumentNullException>(() => PluginCreator.GetCreator(AppDomain.CurrentDomain, null));
    }

    [TestMethod]
    public void CreateShouldLogCatchedExceptionAsError()
    {
      using (MockDomain domain = new MockDomain())
      {
        MockAssemblyRepository repository = new MockAssemblyRepository();
        QualifiedName fakeName = new QualifiedName(
          typeof(string).FullName.Replace("mscorlib", "NonExistingAssemblyName"),
          typeof(string).Assembly.FullName.Replace("mscorlib", "NonExistingAssemblyName"));

        IPluginCreator tested = PluginCreator.GetCreator(domain);
        MockLog mocklog = new MockLog((ILogWriter)tested);
        PluginDescriptor descriptor = MockPluginDescriptor.For(fakeName);
        Exception ex = DoAssert.Throws<PluginException>(() => tested.Create(descriptor, repository, null));
        Assert.IsTrue(mocklog.Any(x => x.Level == MockLog.Level.Error && x.Message.Contains(ex.Message)));
      }
    }

    [TestMethod]
    public void CreateShouldLogCatchedPluginExceptionAsError()
    {
      MockAssemblyRepository repository = new MockAssemblyRepository();
      IPluginCreator tested = PluginCreator.GetCreator();
      MockLog mocklog = new MockLog((ILogWriter)tested);
      PluginDescriptor descriptor = MockPluginDescriptor.For<MockPlugin2>();
      Exception ex = DoAssert.Throws<PluginSettingException>(() => tested.Create(descriptor, repository, null));
      Assert.IsTrue(mocklog.Any(x => x.Level == MockLog.Level.Error && x.Message.Contains(ex.Message)));
    }

    [TestMethod]
    public void ShouldLogInfoEventWithDomainNameOnNewPluginCreator()
    {
      using (MockDomain domain = new MockDomain())
      {
        MockAssemblyRepository repository = new MockAssemblyRepository();
        MockLog mocklog = new MockLog(typeof(PluginCreator));
        var mockLogFactory = new Mock<ILoggerFactory>();
        mockLogFactory.Setup(x => x.GetLog(typeof(PluginCreator).FullName)).Returns(mocklog);
        PluginCreator.GetCreator(domain, mockLogFactory.Object);
        Assert.IsTrue(mocklog.Any(x => x.Level == MockLog.Level.Info && x.Message.Contains(domain.Domain.FriendlyName)));        
      }      
    }

    [TestMethod]
    public void ShouldLogInfoMessageWhenPluginIsCreated()
    {
      using (MockDomain domain = new MockDomain())
      {
        MockAssemblyRepository repository = new MockAssemblyRepository();
        var creator = PluginCreator.GetCreator(domain);
        MockLog mocklog = new MockLog(creator as ILogWriter);
        var descriptor = MockPluginDescriptor.For<MockPlugin1>();
        creator.Create(descriptor, repository, null);
        Assert.IsTrue(mocklog.Any(x => x.Level == MockLog.Level.Info && x.Message.Contains(typeof(MockPlugin1).FullName)));
      }      
    }

    #endregion
  }
}
