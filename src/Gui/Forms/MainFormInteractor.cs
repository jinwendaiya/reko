#region License
/* 
 * Copyright (C) 1999-2018 John Källén.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; see the file COPYING.  If not, write to
 * the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
 */
#endregion

using Reko.Core;
using Reko.Core.Assemblers;
using Reko.Core.Configuration;
using Reko.Core.Output;
using Reko.Core.Serialization;
using Reko.Core.Services;
using Reko.Core.Types;
using Reko.Scanning;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Reko.Gui.Forms
{
    /// <summary>
    /// Provides a component Container implementation, and specifically handles interactions 
    /// with the MainForm. This decouples platform-specific code from the user interaction 
    /// code. This will make it easier to port to other GUI platforms.
    /// </summary>
    public class MainFormInteractor :
        ICommandTarget,
        DecompilerHost
    {
        private IDecompilerShellUiService uiSvc;
        private IMainForm form;
        private IDecompilerService decompilerSvc;
        private IDiagnosticsService diagnosticsSvc;
        private ISearchResultService srSvc;
        private IWorkerDialogService workerDlgSvc;
        private IProjectBrowserService projectBrowserSvc;
        private IDialogFactory dlgFactory;
        private ITabControlHostService searchResultsTabControl;
        private ILoader loader;

        private IPhasePageInteractor currentPhase;
        private InitialPageInteractor pageInitial;
        private IScannedPageInteractor pageScanned;
        private IAnalyzedPageInteractor pageAnalyzed;
        private IFinalPageInteractor pageFinal;

        private MruList mru;
        private string projectFileName;
        private IServiceContainer sc;
        private IConfigurationService config;
        private ICommandTarget subWindowCommandTarget;
        private static string dirSettings;

        private const int MaxMruItems = 9;

        public MainFormInteractor(IServiceProvider services)
        {
            this.dlgFactory = services.RequireService<IDialogFactory>();
            this.mru = new MruList(MaxMruItems);
            this.mru.Load(MruListFile);
            this.sc = services.RequireService<IServiceContainer>();
        }

        public IServiceProvider Services { get { return sc; } }

        public void Attach(IMainForm mainForm)
        {
            this.form = mainForm;

            uiSvc = sc.RequireService<IDecompilerShellUiService>();
            subWindowCommandTarget = uiSvc;

            var svcFactory = sc.RequireService<IServiceFactory>();
            CreateServices(svcFactory, sc);
            CreatePhaseInteractors(svcFactory);
            projectBrowserSvc.Clear();

            var uiPrefsSvc = sc.RequireService<IUiPreferencesService>();
            // It's ok if we can't load settings, just proceed with defaults.
            try
            {
                uiPrefsSvc.Load();
                if (uiPrefsSvc.WindowSize != new System.Drawing.Size())
                    form.Size = uiPrefsSvc.WindowSize;
                form.WindowState = uiPrefsSvc.WindowState;
            }
            catch { };
            SwitchInteractor(pageInitial);
            form.UpdateToolbarState();

            form.Closed += this.MainForm_Closed;

            //form.InitialPage.IsDirtyChanged += new EventHandler(InitialPage_IsDirtyChanged);//$REENABLE
            //MainForm.InitialPage.IsDirty = false;         //$REENABLE

            UpdateWindowTitle();
        }

        private void CreatePhaseInteractors(IServiceFactory svcFactory)
        {
            pageInitial =  svcFactory.CreateInitialPageInteractor();
            pageScanned = svcFactory.CreateScannedPageInteractor();
            pageAnalyzed = new AnalyzedPageInteractorImpl(sc);
            pageFinal = new FinalPageInteractor(sc);
        }

        public virtual IDecompiler CreateDecompiler(ILoader ldr)
        {
            return new DecompilerDriver(ldr, sc);
        }

        private void CreateServices(IServiceFactory svcFactory, IServiceContainer sc)
        {
            sc.AddService<DecompilerHost>(this);

            config = svcFactory.CreateDecompilerConfiguration();
            sc.AddService(typeof(IConfigurationService), config);

            var cmdFactory = new Commands.CommandFactory(sc);
            sc.AddService<ICommandFactory>(cmdFactory);

            var sbSvc = svcFactory.CreateStatusBarService();
            sc.AddService<IStatusBarService>(sbSvc);

            diagnosticsSvc = svcFactory.CreateDiagnosticsService();
            sc.AddService(typeof(IDiagnosticsService), diagnosticsSvc);

            decompilerSvc = svcFactory.CreateDecompilerService();
            sc.AddService(typeof(IDecompilerService), decompilerSvc);

            sc.AddService(typeof(IDecompilerUIService), uiSvc);

            var codeViewSvc = svcFactory.CreateCodeViewerService();
            sc.AddService<ICodeViewerService>(codeViewSvc);

            var segmentViewSvc = svcFactory.CreateImageSegmentService();
            sc.AddService(typeof(ImageSegmentService), segmentViewSvc);

            var del = svcFactory.CreateDecompilerEventListener();
            workerDlgSvc = (IWorkerDialogService)del;
            sc.AddService(typeof(IWorkerDialogService), workerDlgSvc);
            sc.AddService(typeof(DecompilerEventListener), del);

            loader = svcFactory.CreateLoader();
            sc.AddService<ILoader>(loader);

            var abSvc = svcFactory.CreateArchiveBrowserService();
            sc.AddService<IArchiveBrowserService>(abSvc);

            sc.AddService<ILowLevelViewService>(svcFactory.CreateMemoryViewService());
            sc.AddService<IDisassemblyViewService>(svcFactory.CreateDisassemblyViewService());

            var tlSvc = svcFactory.CreateTypeLibraryLoaderService();
            sc.AddService<ITypeLibraryLoaderService>(tlSvc);

            this.projectBrowserSvc = svcFactory.CreateProjectBrowserService();
            sc.AddService<IProjectBrowserService>(projectBrowserSvc);

            var upSvc = svcFactory.CreateUiPreferencesService();
            sc.AddService<IUiPreferencesService>(upSvc);

            var fsSvc = svcFactory.CreateFileSystemService();
            sc.AddService<IFileSystemService>(fsSvc);

            srSvc = svcFactory.CreateSearchResultService();
            sc.AddService<ISearchResultService>(srSvc);

            this.searchResultsTabControl = svcFactory.CreateTabControlHost();
            sc.AddService<ITabControlHostService>(this.searchResultsTabControl);

            var resEditService = svcFactory.CreateResourceEditorService();
            sc.AddService<IResourceEditorService>(resEditService);

            var cgvSvc = svcFactory.CreateCallGraphViewService();
            sc.AddService<ICallGraphViewService>(cgvSvc);

            var viewImpSvc = svcFactory.CreateViewImportService();
            sc.AddService<IViewImportsService>(viewImpSvc);

            var symLdrSvc = svcFactory.CreateSymbolLoadingService();
            sc.AddService<ISymbolLoadingService>(symLdrSvc);
        }

        public virtual TextWriter CreateTextWriter(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return StreamWriter.Null;
            var fsSvc = Services.RequireService<IFileSystemService>();
            return new StreamWriter(fsSvc.CreateFileStream(filename, FileMode.Create, FileAccess.Write), new UTF8Encoding(false));
        }

        public IPhasePageInteractor CurrentPhase
        {
            get { return currentPhase; }
            set { currentPhase = value; }
        }

        public IMainForm MainForm
        {
            get { return form; }
        }

        public void OpenBinary(string file)
        {
            OpenBinary(file, (f) => pageInitial.OpenBinary(f));
        }

        /// <summary>
        /// Master function for opening a new project.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="openAction"></param>
        public void OpenBinary(string file, Func<string,bool> openAction)
        {
            try
            {
                CloseProject();
                SwitchInteractor(InitialPageInteractor);
                if (openAction(file))
                {
                    ProjectFileName = file;
                }
            }
            catch (Exception ex)
            {
                Debug.Print("Caught exception: {0}\r\n{1}", ex.Message, ex.StackTrace);
                uiSvc.ShowError(ex, "Couldn't open file '{0}'.", file);
            }
            finally
            {
                Services.RequireService<IStatusBarService>().SetText("");
        }
        }

        public void OpenBinaryWithPrompt()
        {
            var uiSvc = Services.RequireService<IDecompilerShellUiService>();
            var fileName = uiSvc.ShowOpenFileDialog(null);
            if (fileName != null)
            {
                mru.Use(fileName);
                uiSvc.WithWaitCursor(() => OpenBinary(fileName, (f) => pageInitial.OpenBinary(f)));
                }
            }

        /// <summary>
        /// Prompts the user for a metadata file and adds to the project.
        /// </summary>
        public void AddMetadataFile()
        {
            var fileName = uiSvc.ShowOpenFileDialog(null);
            if (fileName == null)
                return;
            mru.Use(fileName);
            var projectLoader = new ProjectLoader(
                Services,
                loader,
                this.decompilerSvc.Decompiler.Project,
                this.sc.RequireService<DecompilerEventListener>());

            try
            {
                var metadata = projectLoader.LoadMetadataFile(fileName);
                decompilerSvc.Decompiler.Project.MetadataFiles.Add(metadata);
            }
            catch (Exception e)
            {
                uiSvc.ShowError(e, "An error occured while parsing the metadata file {0}", fileName);
            }
        }

        public bool AssembleFile()
        {
            IAssembleFileDialog dlg = null;
            try
            {
                dlg = dlgFactory.CreateAssembleFileDialog();
                dlg.Services = sc;
                if (uiSvc.ShowModalDialog(dlg) != DialogResult.OK)
                    return true;
                mru.Use(dlg.FileName.Text);

                var typeName = dlg.SelectedArchitectureTypeName;
                var t = Type.GetType(typeName, true);
                var asm = (Assembler) t.GetConstructor(Type.EmptyTypes).Invoke(null);
                OpenBinary(dlg.FileName.Text, (f) => pageInitial.Assemble(f, asm));
            }
            catch (Exception e)
            {
                uiSvc.ShowError(e, "An error occurred while assembling {0}.", dlg.FileName.Text);
            }
            return true;
        }

        public bool OpenBinaryAs()
        {
            IOpenAsDialog dlg = null;
            IProcessorArchitecture arch = null;
            try
            {
                dlg = dlgFactory.CreateOpenAsDialog();
                dlg.Services = sc;
                dlg.ArchitectureOptions = new Dictionary<string, object>();
                if (uiSvc.ShowModalDialog(dlg) != DialogResult.OK)
                    return true;

                var rawFileOption = (ListOption)dlg.RawFileTypes.SelectedValue;
                string archName = null;
                string cpuModel = null;
                string envName = null;
                string sAddr = null;
                string loader = null;
                EntryPointElement entry = null;
                if (rawFileOption != null && rawFileOption.Value != null)
                {
                    RawFileElement raw = null;
                    raw = (RawFileElement)rawFileOption.Value;
                    loader = raw.Loader;
                    archName = raw.Architecture;
                    envName = raw.Environment;
                    sAddr = raw.BaseAddress;
                    entry = raw.EntryPoint;
                }
                Architecture archOption = dlg.GetSelectedArchitecture();
                OperatingEnvironment envOption = dlg.GetSelectedEnvironment();
                archName = archName ?? archOption?.Name;
                envName = envName ?? envOption?.Name;
                sAddr = sAddr ?? dlg.AddressTextBox.Text.Trim();
                arch = config.GetArchitecture(archName);
                if (arch == null)
                    throw new InvalidOperationException(string.Format("Unable to load {0} architecture.", archName));
                arch.LoadUserOptions(dlg.ArchitectureOptions);
                if (!arch.TryParseAddress(sAddr, out var addrBase))
                    throw new ApplicationException(string.Format("'{0}' doesn't appear to be a valid address.", sAddr));
                var details = new LoadDetails
                {
                    LoaderName = loader,
                    ArchitectureName = archName,
                    ArchitectureOptions = dlg.ArchitectureOptions,
                    PlatformName = envName,
                    LoadAddress = sAddr,
                    EntryPoint = entry,
                };

                OpenBinary(dlg.FileName.Text, (f) =>
                    pageInitial.OpenBinaryAs(
                        f,
                        details));
            }
            catch (Exception ex)
            {
                uiSvc.ShowError(
                    ex,
                    string.Format("An error occurred when opening the binary file {0}.", dlg.FileName.Text));
            }
            return true;
        }

        public void CloseProject()
        {
            if (decompilerSvc.Decompiler != null && decompilerSvc.Decompiler.Project != null)
            {
                if (uiSvc.Prompt("Do you want to save any changes made to the decompiler project?"))
                {
                    if (!Save())
                        return;
                }
            }

            CloseAllDocumentWindows();
            sc.RequireService<IProjectBrowserService>().Clear();
            diagnosticsSvc.ClearDiagnostics();
            decompilerSvc.Decompiler = null;
        }

        private void CloseAllDocumentWindows()
        {
            foreach (var frame in uiSvc.DocumentWindows.ToArray())
            {
                frame.Close();
            }
        }

        public InitialPageInteractor InitialPageInteractor
        {
            get { return pageInitial; }
        }

        public IScannedPageInteractor ScannedPageInteractor
        {
            get { return pageScanned; }
        }

        public IAnalyzedPageInteractor AnalyzedPageInteractor
        {
            get { return pageAnalyzed; }
        }

        public IFinalPageInteractor FinalPageInteractor
        {
            get { return pageFinal; }
        }

        private static string MruListFile
        {
            get { return Path.Combine(SettingsDirectory, "mru.txt"); }
        }

        public void RestartRecompilation()
        {
            if (decompilerSvc.Decompiler == null ||
                decompilerSvc.Decompiler.Project == null)
                return;

            foreach (var program in decompilerSvc.Decompiler.Project.Programs)
            {
                program.Reset();
            }
            SwitchInteractor(this.InitialPageInteractor);
            
            CloseAllDocumentWindows();
            diagnosticsSvc.ClearDiagnostics();
            projectBrowserSvc.Reload();
        }

        public void NextPhase()
        {
            try
            {
                IPhasePageInteractor next = NextPage(CurrentPhase);

                if (next != null)
                {
                    SwitchInteractor(next);
                }
            }
            catch (Exception ex)
            {
                uiSvc.ShowError(ex, "Unable to proceed.");
            }
            workerDlgSvc.FinishBackgroundWork();
        }

        private IPhasePageInteractor NextPage(IPhasePageInteractor phase)
        {
            IPhasePageInteractor next = null;
            if (phase == pageInitial)
            {
                next = pageScanned;
            }
            else if (phase == pageScanned)
            {
                next = pageAnalyzed;
            }
            else if (phase == pageAnalyzed)
            {
                next = pageFinal;
            }
            return next;
        }

        public void FinishDecompilation()
        {
            try
            {
                IPhasePageInteractor prev = CurrentPhase;
                workerDlgSvc.StartBackgroundWork("Finishing decompilation.", delegate()
                {
                    for (;;)
                    {
                        var next = NextPage(prev);
                        if (next == null)
                            break;
                        next.PerformWork(workerDlgSvc);
                        prev = next;
                    }
                });
                prev.EnterPage();
                CurrentPhase = prev;
                projectBrowserSvc.Reload();
            }
            catch (Exception ex)
            {
                uiSvc.ShowError(ex, "An error occurred while finishing decompilation.");
            }
            workerDlgSvc.FinishBackgroundWork();
        }

        public void LayoutMdi(DocumentWindowLayout layout)
        {
            form.LayoutMdi(layout);
        }

        public void ShowAboutBox()
        {
            using (IAboutDialog dlg = dlgFactory.CreateAboutDialog())
            {
                uiSvc.ShowModalDialog(dlg);
            }
        }

        public string ProjectFileName
        {
            get { return projectFileName; }
            set { projectFileName = value; }
        }

        public void EditFind()
        {
            using (ISearchDialog dlg = dlgFactory.CreateSearchDialog())
            {
                if (uiSvc.ShowModalDialog(dlg) == DialogResult.OK)
                {
                    Func<int, Program, bool> filter = GetScannedFilter(dlg);
                    var re = Core.Dfa.Automaton.CreateFromPattern(dlg.Patterns.Text);
                    if (re == null)
                        return;
                    var hits = this.decompilerSvc.Decompiler.Project.Programs
                        .SelectMany(program => 
                            program.SegmentMap.Segments.Values.SelectMany(seg =>
                            {
                                var linBaseAddr = seg.MemoryArea.BaseAddress.ToLinear();
                                return re.GetMatches(
                                        seg.MemoryArea.Bytes,
                                        0,
                                        (int)seg.MemoryArea.Length)
                                    .Where(o => filter(o, program))
                                    .Select(offset => new AddressSearchHit
                                    {
                                        Program = program,
                                        Address = program.SegmentMap.MapLinearAddressToAddress(
                                            linBaseAddr + (ulong)offset)
                                    });
                            }));
                    srSvc.ShowAddressSearchResults(hits, new CodeSearchDetails());
                }
            }
        }

        private Func<int, Program, bool> GetScannedFilter(ISearchDialog dlg)
        {
            if (dlg.ScannedMemory.Checked)
            {
                if (dlg.UnscannedMemory.Checked)
                    return (o, map) => true;
                else
                    return (o, program) =>
                    {
                        var addr = program.SegmentMap.MapLinearAddressToAddress(
                            (ulong)
                             ((long)program.SegmentMap.BaseAddress.ToLinear() + o));
                        return program.ImageMap.TryFindItem(addr, out var item)
                            && item.DataType != null &&
                            !(item.DataType is UnknownType);
                    };
            }
            else if (dlg.UnscannedMemory.Checked)
            {
                return (o, program) =>
                    {
                        var addr = program.SegmentMap.MapLinearAddressToAddress(
                              (uint)((long) program.SegmentMap.BaseAddress.ToLinear() + o));
                        return program.ImageMap.TryFindItem(addr, out var item)
                            && item.DataType == null ||
                            item.DataType is UnknownType;
                    };
            }
            else
                throw new NotSupportedException();
        }

        public void FindProcedures(ISearchResultService svc)
        {
            var hits = this.decompilerSvc.Decompiler.Project.Programs
                .SelectMany(program => program.Procedures.Select(proc =>
                    new ProcedureSearchHit(program, proc.Key, proc.Value)))
                .ToList();
            svc.ShowSearchResults(new ProcedureSearchResult(this.sc, hits));
        }

        public void FindStrings(ISearchResultService srSvc)
        {
            using (var dlgStrings = dlgFactory.CreateFindStringDialog())
            {
                if (uiSvc.ShowModalDialog(dlgStrings) == DialogResult.OK)
                {
                    var criteria = dlgStrings.GetCriteria();
                    var hits = this.decompilerSvc.Decompiler.Project.Programs
                        .SelectMany(p => new StringFinder(p).FindStrings(criteria));
                    srSvc.ShowAddressSearchResults(
                       hits,
                       new StringSearchDetails(criteria.Encoding));
                }
            }
        }

        public void ViewDisassemblyWindow()
        {
            //$TODO: these need " current program"  to work.
            //var dasmService = sc.GetService<IDisassemblyViewService>();
            //dasmService.ShowWindow();
        }

        public void ViewMemoryWindow()
        {
            //var memService = sc.GetService<ILowLevelViewService>();
            ////$TODO: determine "current program".
            //memService.ViewImage(this.decompilerSvc.Decompiler.Project.Programs.First());
            //memService.ShowWindow();
        }

        public void ViewCallGraph()
        {
            var brSvc = sc.RequireService<IProjectBrowserService>();
            var program = brSvc.CurrentProgram;
            if (program != null)
            {
                var cgvSvc = sc.RequireService<ICallGraphViewService>();
                var title = string.Format("{0} {1}", program.Name, Resources.CallGraphTitle);
                cgvSvc.ShowCallgraph(program, title);
            }
        }

        public void ToolsOptions()
        {
            using (var dlg = dlgFactory.CreateUserPreferencesDialog())
            {
                if (uiSvc.ShowModalDialog(dlg) == DialogResult.OK)
                {
                    var uiPrefsSvc = Services.RequireService<IUiPreferencesService>();
                    uiPrefsSvc.WindowSize = form.Size;
                    uiPrefsSvc.WindowState = form.WindowState;
                    uiPrefsSvc.Save();
                }
            }
        }

        public void ToolsKeyBindings()
        {
            
            using (var dlg = dlgFactory.CreateKeyBindingsDialog(uiSvc.KeyBindings))
            {
                if (uiSvc.ShowModalDialog(dlg) == DialogResult.OK)
                {
                    uiSvc.KeyBindings = dlg.KeyBindings; 
                    //$TODO: save in user settings.
                }
            }
        }

        /// <summary>
        /// Saves the project. 
        /// </summary>
        /// <returns>False if the user cancelled the save, true otherwise.</returns>
        public bool Save()
        {
            if (decompilerSvc.Decompiler == null)
                return true;
            if (string.IsNullOrEmpty(this.ProjectFileName))
            {
                string newName = uiSvc.ShowSaveFileDialog(
                    Path.ChangeExtension(
                        decompilerSvc.Decompiler.Project.Programs[0].Filename,
                        Project_v3.FileExtension));
                if (newName == null)
                    return false;
                ProjectFileName = newName;
                mru.Use(newName);
            }

            var fsSvc = Services.RequireService<IFileSystemService>();
            using (var xw = fsSvc.CreateXmlWriter(ProjectFileName))
            {
                var saver = new ProjectSaver(sc);
                var sProject = saver.Serialize(ProjectFileName, decompilerSvc.Decompiler.Project);
                saver.Save(sProject, xw);
            }
            return true;
        }

        private static string SettingsDirectory
        {
            get
            {
                if (dirSettings == null)
                {
                    string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    dir = Path.Combine(dir, "reko");
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    dirSettings = dir;
                }
                return dirSettings;
            }
        }

        public void SwitchInteractor(IPhasePageInteractor interactor)
        {
            if (interactor == CurrentPhase)
                return;

            if (CurrentPhase != null)
            {
                if (!CurrentPhase.LeavePage())
                    return;
            }
            CurrentPhase = interactor;
            workerDlgSvc.StartBackgroundWork("Entering next phase...", delegate()
            {
                interactor.PerformWork(workerDlgSvc);
            });
            interactor.EnterPage();
            form.UpdateToolbarState();
        }

        public void UpdateWindowTitle()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(decompilerSvc.ProjectName))
            {
                sb.Append(decompilerSvc.ProjectName);
                //$REFACTOR: dirtiness of project is not limited to first page.
                //if (MainForm.InitialPage.IsDirty)
                //    sb.Append('*');
                sb.Append(" - ");
            }
            sb.AppendFormat(
                "Reko Decompiler ({0})", 
                Environment.Is64BitProcess ? "64-bit" : "32-bit");
            MainForm.TitleText = sb.ToString();
        }

        #region ICommandTarget members

        /// <summary>
        /// Determines a command target that should be handling commands. This 
        /// is in essence the "router" that routes commands.
        /// </summary>
        /// <returns></returns>
        private ICommandTarget GetSubCommandTarget()
        {
            if (searchResultsTabControl.ContainsFocus)
                return searchResultsTabControl;
            if (projectBrowserSvc.ContainsFocus)
                return projectBrowserSvc;
            return subWindowCommandTarget;
        }

        public bool QueryStatus(CommandID cmdId, CommandStatus cmdStatus, CommandText cmdText)
        {
            var ct = GetSubCommandTarget();
            if (ct != null && ct.QueryStatus(cmdId, cmdStatus, cmdText))
                return true;
            
            if (currentPhase != null && currentPhase.QueryStatus(cmdId, cmdStatus, cmdText))
                return true;
            if (cmdId.Guid == CmdSets.GuidReko)
            {
                if (QueryMruItem(cmdId.ID, cmdStatus, cmdText))
                    return true;

                switch (cmdId.ID)
                {
                case CmdIds.FileOpen:
                case CmdIds.FileExit:
                case CmdIds.FileOpenAs:
                case CmdIds.FileAssemble:
                case CmdIds.ToolsOptions:
                case CmdIds.WindowsCascade: 
                case CmdIds.WindowsTileVertical:
                case CmdIds.WindowsTileHorizontal:
                case CmdIds.WindowsCloseAll: 
                case CmdIds.HelpAbout: 
                    cmdStatus.Status = MenuStatus.Enabled | MenuStatus.Visible;
                    return true;
                    //$TODO: finish implementing this :)
                case CmdIds.ToolsKeyBindings:
                    cmdStatus.Status = MenuStatus.Visible;
                    return true;
                case CmdIds.FileMru:
                    cmdStatus.Status = MenuStatus.Visible;
                    return true;
                case CmdIds.ActionRestartDecompilation:
                    cmdStatus.Status = MenuStatus.Enabled | MenuStatus.Visible;
                    break;
                case CmdIds.ActionNextPhase:
                    cmdStatus.Status = currentPhase.CanAdvance
                        ? MenuStatus.Enabled | MenuStatus.Visible
                        : MenuStatus.Visible;
                    return true;
                case CmdIds.FileAddBinary:
                case CmdIds.FileAddMetadata:
                case CmdIds.FileSave:
                case CmdIds.FileCloseProject:
                case CmdIds.EditFind:
                case CmdIds.ViewCallGraph:
                case CmdIds.ViewFindAllProcedures:
                case CmdIds.ViewFindStrings:
                    cmdStatus.Status = IsDecompilerLoaded
                        ? MenuStatus.Enabled | MenuStatus.Visible
                        : MenuStatus.Visible;
                    return true;
                }
            }
            return false;
        }

        private bool QueryMruItem(int cmdId, CommandStatus cmdStatus, CommandText cmdText)
        {
            int iMru = cmdId - CmdIds.FileMru;
            if (0 <= iMru && iMru < mru.Items.Count)
            {
                cmdStatus.Status = MenuStatus.Visible | MenuStatus.Enabled;
                cmdText.Text = string.Format("&{0} {1}", iMru+1, mru.Items[iMru]);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Dispatches menu commands.
        /// </summary>
        /// <param name="cmdSet"></param>
        /// <param name="cmdId"></param>
        /// <returns></returns>
        public bool Execute(CommandID cmdId)
        {
            var ct = GetSubCommandTarget();
            if (ct != null && ct.Execute(cmdId))
            {
                form.UpdateToolbarState();
                return true;
            }
            if (currentPhase != null && currentPhase.Execute(cmdId))
            {
                form.UpdateToolbarState();
                return true;
            }
            if (cmdId.Guid == CmdSets.GuidReko)
            {
                if (ExecuteMruFile(cmdId.ID))
                {
                    form.UpdateToolbarState();
                    return false;
                }

                bool retval = false;
                switch (cmdId.ID)
                {
                case CmdIds.FileOpen: OpenBinaryWithPrompt(); retval = true; break;
                case CmdIds.FileOpenAs: retval = OpenBinaryAs(); break;
                case CmdIds.FileAssemble: retval = AssembleFile(); break;
                case CmdIds.FileSave: Save(); retval = true; break;
                case CmdIds.FileAddMetadata: AddMetadataFile(); retval = true; break;
                case CmdIds.FileCloseProject: CloseProject(); retval = true; break;
                case CmdIds.FileExit: form.Close(); retval = true; break;

                case CmdIds.ActionRestartDecompilation: RestartRecompilation(); retval = true; break;
                case CmdIds.ActionNextPhase: NextPhase(); retval = true; break;
                case CmdIds.ActionFinishDecompilation: FinishDecompilation(); retval = true; break;

                case CmdIds.EditFind: EditFind(); retval = true; break;

                case CmdIds.ViewDisassembly: ViewDisassemblyWindow(); retval = true; break;
                case CmdIds.ViewMemory: ViewMemoryWindow(); retval = true; break;
                case CmdIds.ViewCallGraph: ViewCallGraph(); retval = true; break;
                case CmdIds.ViewFindAllProcedures: FindProcedures(srSvc); retval = true; break;
                case CmdIds.ViewFindStrings: FindStrings(srSvc); retval = true; break;

                case CmdIds.ToolsOptions: ToolsOptions(); retval = true; break;
                case CmdIds.ToolsKeyBindings: ToolsKeyBindings(); retval = true; break;

                case CmdIds.WindowsCascade: LayoutMdi(DocumentWindowLayout.None); retval = true; break;
                case CmdIds.WindowsTileVertical: LayoutMdi(DocumentWindowLayout.TiledVertical); retval = true; break;
                case CmdIds.WindowsTileHorizontal: LayoutMdi(DocumentWindowLayout.TiledHorizontal); retval = true; break;
                case CmdIds.WindowsCloseAll: CloseAllDocumentWindows(); retval = true; break;

                case CmdIds.HelpAbout: ShowAboutBox(); retval = true; break;
                }
                form.UpdateToolbarState();
                return retval;
            }
            return false;
        }

        private bool ExecuteMruFile(int cmdId)
        {
            int iMru = cmdId - CmdIds.FileMru;
            if (0 <= iMru && iMru < mru.Items.Count)
            {
                string file = mru.Items[iMru];
                OpenBinary(file, (f) => pageInitial.OpenBinary(file));
                mru.Use(file);
                return true;
            }
            return false;
        }

        private bool IsDecompilerLoaded
        {
            get
            {
                if (decompilerSvc.Decompiler == null)
                    return false;
                return decompilerSvc.Decompiler.Project != null;
            }
        }
        #endregion

        #region DecompilerHost Members //////////////////////////////////

        public IConfigurationService Configuration
        {
            get { return config; }
        }

        public TextWriter CreateDecompiledCodeWriter(string fileName)
        {
            return new StreamWriter(fileName, false, new UTF8Encoding(false));
        }

        public void WriteDisassembly(Program program, Action<Formatter> writer)
        {
            using (TextWriter output = CreateTextWriter(program.DisassemblyFilename))
            {
                writer(new TextFormatter(output));
            }
        }

        public void WriteIntermediateCode(Program program, Action<TextWriter> writer)
        {
            using (TextWriter output = CreateTextWriter(program.IntermediateFilename))
            {
                writer(output);
            }
        }

        public void WriteTypes(Program program, Action<TextWriter> writer)
        {
            using (TextWriter output = CreateTextWriter(program.TypesFilename))
            {
                writer(output);
            }
        }

        public void WriteDecompiledCode(Program program, Action<TextWriter> writer)
        {
            using (TextWriter output = CreateTextWriter(program.OutputFilename))
            {
                writer(output);
            }
        }

        public void WriteGlobals(Program program, Action<TextWriter> writer)
        {
            using (TextWriter output = CreateTextWriter(program.GlobalsFilename))
            {
                writer(output);
            }
        }

        #endregion ////////////////////////////////////////////////////

        // Event handlers //////////////////////////////

        private void miFileExit_Click(object sender, System.EventArgs e)
        {
            form.Close();
        }

        private void MainForm_Closed(object sender, System.EventArgs e)
        {
            var uiPrefsSvc = sc.RequireService<IUiPreferencesService>();
            // It's OK if we can't save settings, just discard them.
            try
            {
                uiPrefsSvc.WindowSize = form.Size;
                uiPrefsSvc.WindowState = form.WindowState;
                uiPrefsSvc.Save();
                mru.Save(MruListFile);
            }
            catch { }
        }

        private void InitialPage_IsDirtyChanged(object sender, EventArgs e)
        {
            UpdateWindowTitle();
        }
        }
}
