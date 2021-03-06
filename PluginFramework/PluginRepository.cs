﻿// 
// Copyright (c) 2013, Erik Rydgren, et al. All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//
//  - Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//  - Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the 
//    documentation and/or other materials provided with the distribution.
//  - Neither the name of PluginFramework nor the names of its contributors may be used to endorse or promote products derived from this 
//    software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, 
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL ERIK RYDGREN OR OTHER CONTRIBUTORS 
// BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
// POSSIBILITY OF SUCH DAMAGE.
//
namespace PluginFramework
{
  using System;
  using System.Collections.Generic;
  using PluginFramework.Logging;

  /// <summary>
  /// Implementation of <see cref="IPluginRepository"/>.
  /// </summary>
  public class PluginRepository : IPluginRepository, ILogWriter
  {
    HashSet<IPluginSource> sources;
    HashSet<PluginDescriptor> plugins;
    
    ILog log;
    ILog ILogWriter.Log
    {
      get { return this.log; }
      set { this.log = value; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginRepository"/> class.
    /// </summary>
    public PluginRepository()
    {
      this.InitLog();
      this.sources = new HashSet<IPluginSource>();
      this.plugins = new HashSet<PluginDescriptor>();      
    }

    /// <summary>
    /// Adds a plugin source to the repository.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <exception cref="System.ArgumentNullException">source</exception>
    /// <exception cref="System.ArgumentException">Source already added</exception>
    public void AddPluginSource(IPluginSource source)
    {
      if (source == null)
        throw new ArgumentNullException("source");

      if (this.sources.Contains(source))
        throw new ArgumentException(Resources.SourceAlreadyAdded);

      source.PluginAdded += this.OnPluginAdded;
      source.PluginRemoved += this.OnPluginRemoved;

      this.sources.Add(source);

      this.log.Debug(Resources.SourceAdded, source.GetType().FullName);
    }

    /// <summary>
    /// Removes a plugin source from the repository.
    /// </summary>
    /// <param name="source">The source.</param>
    public void RemovePluginSource(IPluginSource source)
    {
      if (source == null)
        throw new ArgumentNullException("source");

      if (!this.sources.Contains(source))
        throw new ArgumentException(Resources.UnknownSource);

      this.sources.Remove(source);
      source.PluginAdded -= this.OnPluginAdded;
      source.PluginRemoved -= this.OnPluginRemoved;

      this.log.Debug(Resources.SourceRemoved, source.GetType().FullName);
    }

    /// <summary>
    /// Called when a plugin source reports a new plugin.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="PluginEventArgs"/> instance containing the event data.</param>
    private void OnPluginAdded(object sender, PluginEventArgs e)
    {
      this.log.Info(Resources.AddedPlugin, e.Plugin.QualifiedName.TypeFullName);
      this.plugins.Add(e.Plugin);
    }

    /// <summary>
    /// Called when a plugin source reports a lost plugin.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="PluginEventArgs"/> instance containing the event data.</param>
    private void OnPluginRemoved(object sender, PluginEventArgs e)
    {
      this.log.Info(Resources.RemovedPlugin, e.Plugin.QualifiedName.TypeFullName);
      this.plugins.Remove(e.Plugin);
    }

    /// <summary>
    /// Method for querying for plugins that satisfies a supplied filter
    /// </summary>
    /// <param name="filter">The requirements that plugins must fulfill to be returned. If no filter is provided then all known plugins is returned.</param>
    /// <returns>
    /// Enumerable of plugins that fulfills the requirements
    /// </returns>
    public IEnumerable<PluginDescriptor> Plugins(PluginFilter filter)
    {
      this.log.Debug(Resources.ReturningPluginsFor, filter);
      return filter != null ? filter.Filter(this.plugins) : this.plugins;
    }
  }
}
