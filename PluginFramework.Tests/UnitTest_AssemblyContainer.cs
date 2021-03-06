﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.IO;
using System.Threading;
using PluginFramework.Tests.Mocks;
using PluginFramework.Logging;
using Moq;
using System.Text.RegularExpressions;

namespace PluginFramework.Tests
{
  [TestClass]
  public class UnitTest_AssemblyContainer
  {
    #region Add
    [TestMethod]
    public void AddRequiresArgument()
    {
      AssemblyContainer tested = new AssemblyContainer();
      DoAssert.Throws<ArgumentNullException>(() => tested.Add(null));
    }
    
    [TestMethod]
    public void AddRequiresExistingFile()
    {
      AssemblyContainer tested = new AssemblyContainer();
      DoAssert.Throws<FileNotFoundException>(() => tested.Add(@"c:\" + Guid.NewGuid().ToString()));
    }

    [TestMethod]
    public void AddShouldHandleInvalidFile()
    {
      AssemblyContainer tested = new AssemblyContainer();
      string filename = Guid.NewGuid().ToString() + ".dll";
      using (var output = File.CreateText(filename))
        output.WriteLine("Invalid assembly data");

      try
      {
        var returned = tested.Add(filename);
        Assert.IsFalse(returned);
      }
      finally
      {
        File.Delete(filename);
      }
    }

    [TestMethod]
    public void AddShouldHandleLockedFile()
    {
      AssemblyContainer tested = new AssemblyContainer();
      string filename = Guid.NewGuid().ToString() + ".dll";
      try
      {
        using (var output = File.CreateText(filename))
        {
          var returned = tested.Add(filename);
          Assert.IsFalse(returned);
        }
      }
      finally
      {
        File.Delete(filename);
      }
    }

    [TestMethod]
    public void AddShouldRaiseAssemblyAddedForValidAssembly()
    {
      AssemblyContainer tested = new AssemblyContainer();
      bool raisedAssemblyAdded = false;

      tested.AssemblyAdded += (s, e) => raisedAssemblyAdded = true;
      tested.Add(GetType().Assembly.Location);

      Assert.IsTrue(raisedAssemblyAdded);
    }

    [TestMethod]
    public void AddFailsIfAddingSameAssemblyTwice()
    {
      AssemblyContainer tested = new AssemblyContainer();
      tested.Add(GetType().Assembly.Location);
      DoAssert.Throws<ArgumentException>(() => tested.Add(GetType().Assembly.Location));
    }
    #endregion

    #region Remove
    [TestMethod]
    public void RemoveRequiresArgument()
    {
      AssemblyContainer tested = new AssemblyContainer();
      DoAssert.Throws<ArgumentNullException>(() => tested.Remove(null));
    }

    [TestMethod]
    public void RemoveHandlesUnknownAssembly()
    {
      try
      {
        AssemblyContainer tested = new AssemblyContainer();
        tested.Remove(GetType().Assembly.Location);
      }
      catch
      {
        Assert.Fail();
      }
    }

    [TestMethod]
    public void RemoveShouldRaiseAssemblyRemovedForKnownAssembly()
    {
      AssemblyContainer tested = new AssemblyContainer();
      tested.Add(GetType().Assembly.Location);

      bool raisedAssemblyRemoved = false;
      tested.AssemblyRemoved += (s, e) => raisedAssemblyRemoved = true;
      tested.Remove(GetType().Assembly.Location);

      Assert.IsTrue(raisedAssemblyRemoved);
    }

    #endregion

    #region AddDir
    [TestMethod]
    public void AddDirRequiresArgument()
    {
      AssemblyContainer tested = new AssemblyContainer();
      DoAssert.Throws<ArgumentNullException>(() => tested.AddDir(null));
    }

    [TestMethod]
    public void AddDirAcceptsValidArgument()
    {
      AssemblyContainer tested = new AssemblyContainer();
      IPluginDirectory pluginDir = new MockPluginDirectory();
      tested.AddDir(pluginDir);
    }

    [TestMethod]
    public void AddDirOnlyAcceptDirectoryOnce()
    {
      AssemblyContainer tested = new AssemblyContainer();
      IPluginDirectory pluginDir = new MockPluginDirectory();
      tested.AddDir(pluginDir);
      DoAssert.Throws<ArgumentException>(() => tested.AddDir(pluginDir));
    }
    #endregion

    #region RemoveDir
    [TestMethod]
    public void RemoveDirRequiresArgument()
    {
      AssemblyContainer tested = new AssemblyContainer();
      DoAssert.Throws<ArgumentNullException>(() => tested.RemoveDir(null));
    }

    [TestMethod]
    public void RemoveDirRequiresKnownDirectory()
    {
      AssemblyContainer tested = new AssemblyContainer();
      IPluginDirectory pluginDir = new MockPluginDirectory();
      DoAssert.Throws<ArgumentException>(() => tested.RemoveDir(pluginDir));
    }

    [TestMethod]
    public void RemoveDirShouldHandleRemovalOfKnownDirectory()
    {
      AssemblyContainer tested = new AssemblyContainer();
      IPluginDirectory pluginDir = new MockPluginDirectory();
      tested.AddDir(pluginDir);
      tested.RemoveDir(pluginDir);
    }
    #endregion

    #region IPluginDirectory Events
    [TestMethod]
    public void ShouldRaiseAssemblyAddedOnFileFound()
    {
      AssemblyContainer tested = new AssemblyContainer();
      MockPluginDirectory pluginDir = new MockPluginDirectory();
      tested.AddDir(pluginDir);

      bool assemblyAddedRaised = false;
      tested.AssemblyAdded += (s, e) => assemblyAddedRaised = true;

      pluginDir.RaiseFileFound(GetType().Assembly.Location);

      Assert.IsTrue(assemblyAddedRaised);
    }

    [TestMethod]
    public void ShouldRaiseAssemblyRemovedOnKnownFileLost()
    {
      AssemblyContainer tested = new AssemblyContainer();
      MockPluginDirectory pluginDir = new MockPluginDirectory();
      tested.AddDir(pluginDir);

      bool assemblyRemovedRaised = false;
      tested.AssemblyRemoved += (s, e) => assemblyRemovedRaised = true;

      pluginDir.RaiseFileFound(GetType().Assembly.Location);
      pluginDir.RaiseFileLost(GetType().Assembly.Location);

      Assert.IsTrue(assemblyRemovedRaised);
    }

    [TestMethod]
    public void ShouldNotRaiseAssemblyRemovedOnUnknownFileLost()
    {
      AssemblyContainer tested = new AssemblyContainer();
      MockPluginDirectory pluginDir = new MockPluginDirectory();
      tested.AddDir(pluginDir);

      bool assemblyRemovedRaised = false;
      tested.AssemblyRemoved += (s, e) => assemblyRemovedRaised = true;

      pluginDir.RaiseFileLost(GetType().Assembly.Location);

      Assert.IsFalse(assemblyRemovedRaised);
    }

    [TestMethod]
    public void ShouldForgetLostFile()
    {
      AssemblyContainer tested = new AssemblyContainer();
      MockPluginDirectory pluginDir = new MockPluginDirectory();
      tested.AddDir(pluginDir);

      int assemblyRemovedRaised = 0;
      tested.AssemblyRemoved += (s, e) => assemblyRemovedRaised++;

      pluginDir.RaiseFileFound(GetType().Assembly.Location);
      pluginDir.RaiseFileLost(GetType().Assembly.Location);
      pluginDir.RaiseFileLost(GetType().Assembly.Location);

      Assert.AreEqual(1, assemblyRemovedRaised);
    }

    [TestMethod]
    public void RemovedDirDoesNotRaiseEvents()
    {
      AssemblyContainer tested = new AssemblyContainer();
      MockPluginDirectory pluginDir = new MockPluginDirectory();
      tested.AddDir(pluginDir);

      int assemblyAddedRaised = 0;
      int assemblyRemovedRaised = 0;
      tested.AssemblyAdded += (s, e) => assemblyAddedRaised++;
      tested.AssemblyRemoved += (s, e) => assemblyRemovedRaised++;

      tested.RemoveDir(pluginDir);
      pluginDir.RaiseFileFound(GetType().Assembly.Location);
      pluginDir.RaiseFileLost(GetType().Assembly.Location);

      Assert.AreEqual(0, assemblyAddedRaised);
      Assert.AreEqual(0, assemblyRemovedRaised);
    }
    #endregion

    #region Fetch
    [TestMethod]
    public void FetchShouldReturnExistingAssembly()
    {
      AssemblyContainer tested = new AssemblyContainer();
      Assembly assembly = Assembly.GetAssembly(typeof(MockPlugin1));
      tested.Add(assembly.Location);
      byte[] returned = tested.Fetch(assembly.FullName);
      Assert.IsNotNull(returned);
    }

    [TestMethod]
    public void FetchShuldReturnNullForUnknownAssembly()
    {
      AssemblyContainer tested = new AssemblyContainer();
      byte[] returned = tested.Fetch(Assembly.GetAssembly(typeof(MockPlugin1)).FullName);
      Assert.IsNull(returned);
    }

    [TestMethod]
    public void FetchShouldReturnNullForUnreadableAssembly()
    {
      AssemblyContainer tested = new AssemblyContainer();
      Assembly assembly = Assembly.GetAssembly(typeof(MockPlugin1));

      string location = assembly.Location.Replace(".dll", ".mock.dll");
      File.Copy(assembly.Location, location, true);

      try
      {
        tested.Add(location);

        using (Stream locking = FileExtension.WaitAndOpen(location, FileMode.Open, FileAccess.Read, FileShare.None, TimeSpan.FromSeconds(5)))
        {
          byte[] returned = tested.Fetch(Assembly.GetAssembly(typeof(MockPlugin1)).FullName);
          Assert.IsNull(returned);
        }
      }
      finally
      {
        File.Delete(location);
      }
    }
    #endregion

    #region Logging
    [TestMethod]
    public void ShouldImplementILogWriter()
    {
      AssemblyContainer tested = new AssemblyContainer();
      Assert.IsInstanceOfType(tested, typeof(ILogWriter));
    }

    [TestMethod]
    public void ConstructorShouldInitLog()
    {
      ILogWriter tested = new AssemblyContainer();
      Assert.IsNotNull(tested.Log);
    }

    [TestMethod]
    public void ShouldLogToInfoWhenAddingDirectory()
    {
      var path = "mockDir";
      var mockPluginDir = new Mock<IPluginDirectory>();
      mockPluginDir.Setup(x => x.Path).Returns(path);

      AssemblyContainer tested = new AssemblyContainer();
      MockLog log = new MockLog(tested);
      tested.AddDir(mockPluginDir.Object);

      Assert.IsTrue(log.Any(x => x.Level == MockLog.Level.Info && x.Message.Contains("Added plugin directory ") && x.Message.Contains(path)));
    }

    [TestMethod]
    public void ShouldLogToInfoWhenRemovingDirectory()
    {
      var path = "mockDir";
      var mockPluginDir = new Mock<IPluginDirectory>();
      mockPluginDir.Setup(x => x.Path).Returns(path);

      AssemblyContainer tested = new AssemblyContainer();
      MockLog log = new MockLog(tested);
      tested.AddDir(mockPluginDir.Object);
      tested.RemoveDir(mockPluginDir.Object);

      Assert.IsTrue(log.Any(x => x.Level == MockLog.Level.Info && x.Message.Contains("Removed plugin directory ") && x.Message.Contains(path)));
    }

    [TestMethod]
    public void ShouldLogToInfoWhenAddingAssembly()
    {
      var path = GetType().Assembly.Location;

      AssemblyContainer tested = new AssemblyContainer();
      MockLog log = new MockLog(tested);
      tested.Add(path);

      Assert.IsTrue(log.Any(x => x.Level == MockLog.Level.Info && x.Message.Contains("Assembly added ") && x.Message.Contains(path)));
    }

    [TestMethod]
    public void ShouldLogToInfoWhenRemovingAssembly()
    {
      var path = GetType().Assembly.Location;

      AssemblyContainer tested = new AssemblyContainer();
      MockLog log = new MockLog(tested);
      tested.Add(path);
      tested.Remove(path);

      Assert.IsTrue(log.Any(x => x.Level == MockLog.Level.Info && x.Message.Contains("Assembly removed ") && x.Message.Contains(path)));
    }

    [TestMethod]
    public void ShouldLogToDebugWhenAssemblyIsFetched()
    {
      var assemblyname = GetType().Assembly.FullName;
      var path = GetType().Assembly.Location;
      var pattern = new Regex(@"^Assembly fetched .+ \(\d+ bytes read from .*\)$");
      AssemblyContainer tested = new AssemblyContainer();
      MockLog log = new MockLog(tested);
      tested.Add(path);
      tested.Fetch(assemblyname);
      Assert.IsTrue(log.Any(x => x.Level == MockLog.Level.Debug && pattern.IsMatch(x.Message) && x.Message.Contains(path) && x.Message.Contains(assemblyname)));
    }

    [TestMethod]
    public void ShouldLogToWarnWhenTryingToFetchUnknownAssembly()
    {
      var assemblyname = GetType().Assembly.FullName;
      var pattern = new Regex(@"^Unable to fetch .+, assembly not known.$");
      AssemblyContainer tested = new AssemblyContainer();
      MockLog log = new MockLog(tested);
      tested.Fetch(GetType().Assembly.FullName);
      Assert.IsTrue(log.Any(x => x.Level == MockLog.Level.Warn && pattern.IsMatch(x.Message) && x.Message.Contains(assemblyname)));
    }

    [TestMethod]
    public void ShouldLogToErrorWhenAllKnownPathsToAssemblyDoesNotExist()
    {      
      var assemblyname = GetType().Assembly.FullName;
      var path = Guid.NewGuid().ToString() + ".dll";
      var pattern = new Regex(@"^Unable to fetch .+, file not found in these locations:$");
      try
      {
        File.Copy(GetType().Assembly.Location, path);
        AssemblyContainer tested = new AssemblyContainer();
        MockLog log = new MockLog(tested);
        tested.Add(path);
        File.Delete(path);
        tested.Fetch(assemblyname);
        Assert.IsTrue(log.Any(x => x.Level == MockLog.Level.Error && pattern.IsMatch(x.Message) && x.Message.Contains(assemblyname)));
        Assert.IsTrue(log.Any(x => x.Level == MockLog.Level.Error && x.Message.StartsWith("  --> ") && x.Message.EndsWith(path)));        
      }
      finally
      {
        File.Delete(path);
      }
    }

    [TestMethod]
    public void FetchShouldLogExceptionsAsErrors()
    {
      var assemblyname = GetType().Assembly.FullName;
      var path = Guid.NewGuid().ToString() + ".dll";
      var pattern = new Regex(@"^Exception while fetching .+ (.+) .*$");
      try
      {
        File.Copy(GetType().Assembly.Location, path);
        AssemblyContainer tested = new AssemblyContainer();
        MockLog log = new MockLog(tested);
        tested.Add(path);
        using (var file = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.None))
        {
          tested.Fetch(assemblyname);
          Assert.IsTrue(log.Any(x => x.Level == MockLog.Level.Error && pattern.IsMatch(x.Message) && x.Message.Contains(assemblyname) && x.Message.Contains(path)));
        }
      }
      finally
      {
        File.Delete(path);
      }
    }

    #endregion
  }
}
