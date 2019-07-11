﻿using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

// contributed by jeremiah.morrill@gmail.com

//http://www.windows-tech.info/4/0311d2b4778d6ebe.php
//Access is denied.
//http://stackoverflow.com/questions/666799/embedding-unmanaged-dll-into-a-managed-c-dll

//"J:\Yosuke\Documents\Visual Studio 2017\Projects\Shazzam\Shazzam\Shazzam\Shazzam_1_TemporaryKey.pfx"
//as of 11JUL2019, the password is boss.

namespace Shazzam.CodeGen {
  internal enum ShaderProfile {
    PixelShader2,
    PixelShader3,
  }
  class ShaderCompiler :INotifyPropertyChanged {
    [Guid("8BA5FB08-5195-40e2-AC58-0D989C3A0102"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ID3DXBuffer {
      [PreserveSig]
      IntPtr GetBufferPointer();
      [PreserveSig]
      int GetBufferSize();
    }

    [PreserveSig]
    [DllImport("D3DX9_40.dll",CharSet = CharSet.Auto)]
    private static extern int D3DXCompileShader([MarshalAs(UnmanagedType.LPStr)]string pSrcData,
                                                int dataLen,
                                                IntPtr pDefines,
                                                IntPtr includes,
                                                [MarshalAs(UnmanagedType.LPStr)]string pFunctionName,
                                                [MarshalAs(UnmanagedType.LPStr)]string pTarget,
                                                int flags,
                                                out ID3DXBuffer ppShader,
                                                out ID3DXBuffer ppErrorMsgs,
                                                out IntPtr ppConstantTable);

    [PreserveSig]
    [DllImport("D3DX9_40_64bit.dll",CharSet = CharSet.Auto,EntryPoint = "D3DXCompileShader")]
    private static extern int D3DXCompileShader64Bit([MarshalAs(UnmanagedType.LPStr)]string pSrcData,
                                                int dataLen,
                                                IntPtr pDefines,
                                                IntPtr includes,
                                                [MarshalAs(UnmanagedType.LPStr)]string pFunctionName,
                                                [MarshalAs(UnmanagedType.LPStr)]string pTarget,
                                                int flags,
                                                out ID3DXBuffer ppShader,
                                                out ID3DXBuffer ppErrorMsgs,
                                                out IntPtr ppConstantTable);

    [PreserveSig]
    [DllImport("d3dx10_43.dll",CharSet = CharSet.Auto)]
    private static extern int D3DX10CompileFromMemory([MarshalAs(UnmanagedType.LPStr)]string pSrcData,
                                                        int dataLen,
                                                        [MarshalAs(UnmanagedType.LPStr)]string pFilename,
                                                        IntPtr pDefines,
                                                        IntPtr pInclude,
                                                        [MarshalAs(UnmanagedType.LPStr)]string pFunctionName,
                                                        [MarshalAs(UnmanagedType.LPStr)]string pProfile,
                                                        int flags1,
                                                        int flags2,
                                                        IntPtr pPump,
                                                        out ID3DXBuffer ppShader,
                                                        out ID3DXBuffer ppErrorMsgs,
                                                        ref int pHresult);

    [PreEmptive.Attributes.Feature("CompileShader",EventType = PreEmptive.Attributes.FeatureEventTypes.Start)]
    public void Compile(string codeText,ShaderProfile shaderProfile) {
      IsCompiled = false;
      string path = Properties.Settings.Default.FolderPath_Output;

      IntPtr defines = IntPtr.Zero;
      IntPtr includes = IntPtr.Zero;
      IntPtr ppConstantTable = IntPtr.Zero;
      ID3DXBuffer ppShader;
      ID3DXBuffer ppErrorMsgs;

      var methodName = "main";

      string targetProfile = "ps_2_0";
      if (shaderProfile == ShaderProfile.PixelShader3) {
        targetProfile = "ps_3_0";
      } else {
        targetProfile = "ps_2_0";
      }

      bool useDx10 = false;

      int hr = 0;
      if (useDx10) {
        int pHr = 0;
        hr = D3DX10CompileFromMemory(codeText,
                                     codeText.Length,
                                     string.Empty,
                                     IntPtr.Zero,
                                     IntPtr.Zero,
                                     methodName,
                                     targetProfile,
                                     0,
                                     0,
                                     IntPtr.Zero,
                                     out ppShader,
                                     out ppErrorMsgs,
                                     ref pHr);
      } else {
        if (System.IntPtr.Size == 8) {
          // 64 bit
          hr = D3DXCompileShader64Bit(codeText,
                                   codeText.Length,
                                   defines,
                                   includes,
                                   methodName,
                                   targetProfile,
                                   0,
                                   out ppShader,
                                   out ppErrorMsgs,
                                   out ppConstantTable);
        } else {
          // 32 bit
          hr = D3DXCompileShader(codeText,
                                   codeText.Length,
                                   defines,
                                   includes,
                                   methodName,
                                   targetProfile,
                                   0,
                                   out ppShader,
                                   out ppErrorMsgs,
                                   out ppConstantTable);
        }
      }
      if (hr != 0) {
        IntPtr errors = ppErrorMsgs.GetBufferPointer();
        int size = ppErrorMsgs.GetBufferSize();
        ErrorText = Marshal.PtrToStringAnsi(errors);
        IsCompiled = false;
        goto finished;
      }
      ErrorText = "";
      IsCompiled = true;
      var psPath = path + Constants.FileNames.TempShaderPs;
      IntPtr pCompiledPs = ppShader.GetBufferPointer();
      int compiledPsSize = ppShader.GetBufferSize();

      var compiledPs = new byte[compiledPsSize];
      Marshal.Copy(pCompiledPs,compiledPs,0,compiledPs.Length);
      using (var psFile = File.Open(psPath,FileMode.Create,FileAccess.Write)) {
        psFile.Write(compiledPs,0,compiledPs.Length);
      }
    finished:
      if (ppShader != null)
        Marshal.ReleaseComObject(ppShader);
      ppShader = null;
      if (ppErrorMsgs != null)
        Marshal.ReleaseComObject(ppErrorMsgs);
      ppErrorMsgs = null;
      CompileFinished();
      //CreateFileCopies(path);
      //	throw new Exception("testing");

    }
    [PreEmptive.Attributes.Feature("CompileShader",EventType = PreEmptive.Attributes.FeatureEventTypes.Stop)]
    private void CompileFinished() {
      // for instrumentation
    }
    //[PreEmptive.Attributes.Feature("CompileShader", EventType = PreEmptive.Attributes.FeatureEventTypes.Stop)]
    //private void CreateFileCopies(string path)
    //{
    //  if (String.IsNullOrEmpty(Properties.Settings.Default.FilePath_LastFx))
    //  {
    //    return;
    //  }
    //  string currentFileName = System.IO.Path.GetFileNameWithoutExtension(Properties.Settings.Default.FilePath_LastFx);
    //  if (File.Exists(path + Constants.FileNames.TempShaderPs))
    //  {

    //    File.Copy(path + Constants.FileNames.TempShaderPs, path + currentFileName + ".ps", true);
    //  }
    //}
    public void Reset() {
      ErrorText = "not compiled";
    }
    private string _errorText;
    public String ErrorText {
      get {
        return _errorText;
      }
      set {
        _errorText = value;
        RaiseNotifyChanged("ErrorText");
      }
    }
    private bool _isCompiled;
    public bool IsCompiled {
      get {
        return _isCompiled;
      }
      set {
        _isCompiled = value;
        RaiseNotifyChanged("IsCompiled");
      }
    }
    private void RaiseNotifyChanged(string propName) {
      if (PropertyChanged != null)
        PropertyChanged(this,new PropertyChangedEventArgs(propName));
    }
    public event PropertyChangedEventHandler PropertyChanged;
  }
}
