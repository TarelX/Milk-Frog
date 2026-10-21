param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing.Common
Add-Type -ReferencedAssemblies @('System.Drawing.Common','System.Drawing.Primitives','System.Private.Windows.GdiPlus','System.Private.Windows.Core','System.Runtime.InteropServices','System.Collections','System.Text.Json') -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections.Generic;
public static class FrogSpriteSplitter {
  public sealed class Frame {
    public string file {get;set;}
    public int[] bounds {get;set;}
    public int keptComponents {get;set;}
  }
  public sealed class Sheet {
    public string id {get;set;}
    public int width {get;set;}
    public int height {get;set;}
    public Frame[] frames {get;set;}
  }
  // Sprite extraction only: keep the generated drawing; remove detached matte specks.
  public static Sheet Split(string source, string output, string id) {
    using(var sheet = new Bitmap(source)) {
      int w=(int)Math.Ceiling(sheet.Width/4.0), h=(int)Math.Ceiling(sheet.Height/2.0);
      var result=new Sheet{id=id,width=w,height=h,frames=new Frame[8]};
      Directory.CreateDirectory(output);
      for(int f=0;f<8;f++) {
        using(var frame=new Bitmap(w,h,PixelFormat.Format32bppArgb)) {
          int left=(int)Math.Round((f%4)*sheet.Width/4.0),top=(int)Math.Round((f/4)*sheet.Height/2.0);
          int right=(int)Math.Round(((f%4)+1)*sheet.Width/4.0),bottom=(int)Math.Round(((f/4)+1)*sheet.Height/2.0);
          using(var g=Graphics.FromImage(frame))g.DrawImage(sheet,new Rectangle(0,0,w,h),new Rectangle(left,top,right-left,bottom-top),GraphicsUnit.Pixel);
          var bits=frame.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
          int stride=bits.Stride, size=stride*h;
          var data=new byte[size];Marshal.Copy(bits.Scan0,data,0,size);
          var labels=new int[w*h];var areas=new List<int>{0};var boxes=new List<int[]>{new int[4]};
          var queue=new int[w*h];int label=0;
          for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
            int i=y*w+x;
            if(labels[i]!=0 || data[y*stride+x*4+3]<40)continue;
            label++;int head=0,tail=0;queue[tail++]=i;labels[i]=label;
            int minX=x,minY=y,maxX=x,maxY=y;
            while(head<tail) {
              int p=queue[head++],py=p/w,px=p%w;
              minX=Math.Min(minX,px);minY=Math.Min(minY,py);maxX=Math.Max(maxX,px);maxY=Math.Max(maxY,py);
              for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++) {
                int nx=px+dx,ny=py+dy;
                if(nx<0||nx>=w||ny<0||ny>=h)continue;
                int ni=ny*w+nx;
                if(labels[ni]==0 && data[ny*stride+nx*4+3]>=40){labels[ni]=label;queue[tail++]=ni;}
              }
            }
            areas.Add(tail);boxes.Add(new int[]{minX,minY,maxX-minX+1,maxY-minY+1});
          }
          int largest=0;for(int l=1;l<areas.Count;l++)if(areas[l]>areas[largest])largest=l;
          if(largest==0)throw new Exception("Empty sprite: "+id+"/"+f);
          int minimum=Math.Max(100,(int)(areas[largest]*0.008)),kept=0;
          var keep=new bool[w*h];
          for(int l=1;l<areas.Count;l++)if(areas[l]>=minimum)kept++;
          // Two-pixel fringe preserves antialiasing around accepted components.
          for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
            int l=labels[y*w+x];if(l==0||areas[l]<minimum)continue;
            for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++) {
              int nx=x+dx,ny=y+dy;if(nx>=0&&nx<w&&ny>=0&&ny<h)keep[ny*w+nx]=true;
            }
          }
          for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(!keep[y*w+x]) {
            int p=y*stride+x*4;data[p]=0;data[p+1]=0;data[p+2]=0;data[p+3]=0;
          }
          Marshal.Copy(data,0,bits.Scan0,size);frame.UnlockBits(bits);
          string name=f.ToString("D2")+".png";frame.Save(Path.Combine(output,name),ImageFormat.Png);
          result.frames[f]=new Frame{file="frames/"+id+"/"+name,bounds=boxes[largest],keptComponents=kept};
        }
      }
      // Normalize the whole action using ONE scale, keeping intentional squash and flight.
      int reference=id=="bath-out"?7:0;
      double scale=390.0/result.frames[reference].bounds[3];
      for(int f=0;f<8;f++) {
        string file=Path.Combine(output,f.ToString("D2")+".png");
        var b=result.frames[f].bounds;
        double baseline=b[1]+b[3];
        if(id=="happy" && (f==3||f==4))baseline=f<4?result.frames[0].bounds[1]+result.frames[0].bounds[3]:result.frames[7].bounds[1]+result.frames[7].bounds[3];
        if(id=="drag" && f>=3)baseline=result.frames[0].bounds[1]+result.frames[0].bounds[3];
        float x=(float)(256-w*scale/2),y=(float)(462-baseline*scale);
        using(var normalized=new Bitmap(512,512,PixelFormat.Format32bppArgb)) {
          using(var frame=new Bitmap(file))using(var g=Graphics.FromImage(normalized)) {
            g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(frame,new RectangleF(x,y,(float)(w*scale),(float)(h*scale)),new RectangleF(0,0,w,h),GraphicsUnit.Pixel);
          }
          normalized.Save(file,ImageFormat.Png);
        }
      }
      result.width=512;result.height=512;
      return result;
    }
  }
}
'@
$sheetRoot=Join-Path $ProjectRoot 'assets\generated\young\animations\sheets'
$frameRoot=Join-Path $ProjectRoot 'assets\generated\young\animations\frames'
$results=@(Get-ChildItem -LiteralPath $sheetRoot -Filter '*.png' | Where-Object { $_.BaseName -ne 'play' } | Sort-Object Name | ForEach-Object {
  [FrogSpriteSplitter]::Split($_.FullName,(Join-Path $frameRoot $_.BaseName),$_.BaseName)
})
$results | ConvertTo-Json -Depth 8 -Compress
