// <file>
//     <owner name="David Srbeck�" email="dsrbecky@post.cz"/>
// </file>

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using DebuggerInterop.Core;
using DebuggerInterop.MetaData;

namespace DebuggerLibrary
{
	public partial class NDebugger
	{
		List<Breakpoint> breakpointCollection = new List<Breakpoint>();

		public event BreakpointEventHandler BreakpointAdded;
		public event BreakpointEventHandler BreakpointRemoved;
		public event BreakpointEventHandler BreakpointStateChanged;
		public event BreakpointEventHandler BreakpointHit;

		protected void OnBreakpointAdded(Breakpoint breakpoint)
		{
			if (BreakpointAdded != null) {
				BreakpointAdded(this, new BreakpointEventArgs(breakpoint));
			}
		}

		protected void OnBreakpointRemoved(Breakpoint breakpoint)
		{
			if (BreakpointRemoved != null) {
				BreakpointRemoved(this, new BreakpointEventArgs(breakpoint));
			}
		}

		protected void OnBreakpointStateChanged(object sender, BreakpointEventArgs e)
		{
			if (BreakpointStateChanged != null) {
				BreakpointStateChanged(this, new BreakpointEventArgs(e.Breakpoint));
			}
		}

		protected void OnBreakpointHit(object sender, BreakpointEventArgs e)
		{
			if (BreakpointHit != null) {
				BreakpointHit(this, new BreakpointEventArgs(e.Breakpoint));
			}
		}

		public IList<Breakpoint> Breakpoints {
			get {
				return breakpointCollection.AsReadOnly();
			}
		}

		internal Breakpoint GetBreakpoint(ICorDebugBreakpoint corBreakpoint)
		{
			foreach(Breakpoint breakpoint in breakpointCollection) {
				if (breakpoint == corBreakpoint) {
					return breakpoint;
				}
			}

			throw new UnableToGetPropertyException(this, "GetBreakpoint(ICorDebugBreakpoint corBreakpoint)", "Breakpoint is not in collection");
		}

		public Breakpoint AddBreakpoint(Breakpoint breakpoint)  
		{
			breakpointCollection.Add(breakpoint);

			breakpoint.SetBreakpoint();
			breakpoint.BreakpointStateChanged += new BreakpointEventHandler(OnBreakpointStateChanged);
			breakpoint.BreakpointHit += new BreakpointEventHandler(OnBreakpointHit);

			OnBreakpointAdded(breakpoint);

			return breakpoint;
		}

		public Breakpoint AddBreakpoint(SourcecodeSegment segment)
		{
			return AddBreakpoint(new Breakpoint(segment));
		}

		public Breakpoint AddBreakpoint(int line)
		{
			return AddBreakpoint(new Breakpoint(line));
		}

		public Breakpoint AddBreakpoint(string sourceFilename, int line)
		{
			return AddBreakpoint(new Breakpoint(sourceFilename, line));
		}

		public Breakpoint AddBreakpoint(string sourceFilename, int line, int column)
		{
			return AddBreakpoint(new Breakpoint(sourceFilename, line, column));
		}

		public void RemoveBreakpoint(Breakpoint breakpoint)  
		{
			breakpoint.BreakpointStateChanged -= new BreakpointEventHandler(OnBreakpointStateChanged);
			breakpoint.BreakpointHit -= new BreakpointEventHandler(OnBreakpointHit);
	
            breakpoint.Enabled = false;
			breakpointCollection.Remove( breakpoint );
			OnBreakpointRemoved( breakpoint);
		}

		internal void ResetBreakpoints()
		{
			foreach (Breakpoint b in breakpointCollection) {
				b.ResetBreakpoint();
			}
		}

		internal void SetBreakpointsInModule(object sender, ModuleEventArgs e) 
		{
            foreach (Breakpoint b in breakpointCollection) {
				b.SetBreakpoint();
			}
		}
	}
}
