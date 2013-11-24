//---------------------------------------------------------------------------
// This software is licensed under the BSD license
// You can read the terms of this license in the
// RatioMaker.nfo file, included in this archive and
// also here: http://www.xfree86.org/3.3.6/COPYRIGHT2.html#5
// *****
// JTS-US can be contacted at: dev (at) moofdev.org
// The original, unmodified source as released by the original
// author can be downloaded here: http:/www.moofdev.org/
//---------------------------------------------------------------------------
















#include <vcl.h>
#pragma hdrstop

#include "RatioMaker.h"
#include <math.h>
//---------------------------------------------------------------------------
#pragma package(smart_init)
#pragma resource "*.dfm"
TForm1 *Form1;
//---------------------------------------------------------------------------
__fastcall TForm1::TForm1(TComponent* Owner)
        : TForm(Owner)
{
}
//---------------------------------------------------------------------------
AnsiString genpeerid(int client) {        // %2DBC0059%2D%A2%1A%3D%FE%9B%A3%A5j0%14C%02
        AnsiString peer_id;
        switch(client) {
                case 0:
                peer_id = "%2DBC0060%2D";
                for(int i=1;i<=12;i++) {
                        peer_id += "\%";
                        peer_id += (new AnsiString)->sprintf("%02X",random(256));
                }
                break;
                case 1:
                peer_id = "%2dUT1300%2d";
                for(int i=1;i<=12;i++) {
                        peer_id += "\%";
                        peer_id += (new AnsiString)->sprintf("%02X",random(256));
                }
                break;
                case 2:
                peer_id = "-AZ2306-";
                for(int i=1;i<=12;i++) {
                        switch(random(3)%3) {
                                case 0: peer_id += IntToStr(random(10)); break;
                                case 1: peer_id += (new AnsiString)->sprintf("%c",random(25) + 65); break;
                                case 2: peer_id += (new AnsiString)->sprintf("%c",random(25) + 97); break;
                        }
                }
                break;
                case 3:
                peer_id = "A301-----";
                for(int i=1;i<=11;i++) {
                        switch(random(3)%3) {
                                case 0: peer_id += IntToStr(random(10)); break;
                                case 1: peer_id += (new AnsiString)->sprintf("%c",random(25) + 65); break;
                                case 2: peer_id += (new AnsiString)->sprintf("%c",random(25) + 97); break;
                        }
                }
                break;
                default:
                peer_id = Form1->Edit3->Text;
                break;
        }

        return peer_id;
}

AnsiString genkey(int client) {
        AnsiString key;
        if(client==0) key = random(99999);
        else for(int i=1;i<=6;i++) {
                        switch(random(3)%3) {
                                case 0: key += random(10); break;
                                case 1: key += (new AnsiString)->sprintf("%c",random(25) + 65); break;
                                case 2: key += (new AnsiString)->sprintf("%c",random(25) + 97); break;
                        }
              }
        return key;
}

AnsiString myurl_start,myurl_gimmeh,myurl_putmeh,myurl_stop;

void http_action() {
        int sizeaiurea = random(9999);
        float procentaj = random(10000);
        AnsiString sa_str;
        AnsiString proc_hash = Form1->Edit6->Text;
        int n = Form1->Edit6->Text.Length();
        for(int i=1;i<=n;i+=2) {
                proc_hash = proc_hash.Insert("\%",n-i);
        }
        sa_str = FloatToStr(floor(StrToFloat(StringReplace(Form1->Edit7->Text,ThousandSeparator,"",TReplaceFlags() << rfReplaceAll)) * procentaj / 10000));
//        Form1->Edit7->Text = sa_str;
        myurl_start = Form1->Edit1->Text;
        myurl_start += "?info_hash=";
        myurl_start += proc_hash;
        myurl_start += "&peer_id=";
        myurl_start += Form1->Edit3->Text;
        myurl_start += "&port=";
        myurl_start += Form1->Edit8->Text;
        myurl_start += "&uploaded=0&downloaded=0&left=";
        myurl_start += sa_str;
        myurl_start += "&numwant=200&compact=1&event=started&no_peer_id=1&key=";
        myurl_start += Form1->Edit4->Text;

        myurl_gimmeh = Form1->Edit1->Text;
        myurl_gimmeh += "?info_hash=";
        myurl_gimmeh += proc_hash;
        myurl_gimmeh += "&peer_id=";
        myurl_gimmeh += Form1->Edit3->Text;
        myurl_gimmeh += "&port=";
        myurl_gimmeh += Form1->Edit8->Text;
        myurl_gimmeh += "&uploaded=";
        myurl_gimmeh += Form1->Edit2->Text * 1024 * 1024 + (double)sizeaiurea;
        myurl_gimmeh += "&downloaded=0&left=";
        myurl_gimmeh += sa_str;
        myurl_gimmeh += "&numwant=200&compact=1&no_peer_id=1&key=";
        myurl_gimmeh += Form1->Edit4->Text;

        myurl_putmeh = Form1->Edit1->Text;
        myurl_putmeh += "?info_hash=";
        myurl_putmeh += proc_hash;
        myurl_putmeh += "&peer_id=";
        myurl_putmeh += Form1->Edit3->Text;
        myurl_putmeh += "&port=";
        myurl_putmeh += Form1->Edit8->Text;
        myurl_putmeh += "&uploaded=0&downloaded=";
        myurl_putmeh += Form1->Edit2->Text * 1024 * 1024 + (double)sizeaiurea;
        myurl_putmeh += "&left=";
        myurl_putmeh += sa_str - Form1->Edit2->Text * 1024 * 1024 + (double)sizeaiurea;
        myurl_putmeh += "&numwant=200&compact=1&no_peer_id=1&key=";
        myurl_putmeh += Form1->Edit4->Text;

        myurl_stop = Form1->Edit1->Text;
        myurl_stop += "?info_hash=";
        myurl_stop += proc_hash;
        myurl_stop += "&peer_id=";
        myurl_stop += Form1->Edit3->Text;
        myurl_stop += "&port=";
        myurl_stop += Form1->Edit8->Text;
        if(Form1->RadioButton1->Checked) {
                myurl_stop += "&uploaded=";
                myurl_stop += Form1->Edit2->Text * 1024 * 1024 + (double)sizeaiurea;
                myurl_stop += "&downloaded=0&left=";
                myurl_stop += sa_str;
                myurl_stop += "&numwant=200&compact=1&event=stopped&no_peer_id=1&key=";
        }
        else {
                myurl_stop += "&uploaded=0&downloaded=";
                myurl_stop += Form1->Edit2->Text * 1024 * 1024 + (double)sizeaiurea;
                myurl_stop += "&left=";
                myurl_stop += sa_str - Form1->Edit2->Text * 1024 * 1024 + (double)sizeaiurea;
                myurl_stop += "&numwant=200&compact=1&event=stopped&no_peer_id=1&key=";
        }
        myurl_stop += Form1->Edit4->Text;

        Form1->Button1->Enabled = FALSE;
/*        Form1->Memo1->Lines->Add("Starting...");
        Form1->Memo1->Lines->Add("Sending event STARTED");
        Form1->Memo1->Lines->Add("URL: ");
        Form1->Memo1->Lines->Add(myurl_start); */
        Form1->IdTCPServer1->DefaultPort = StrToInt(Form1->Edit8->Text);
        Form1->IdTCPServer1->Active = 1;
/*        Form1->Memo1->Lines->Add("Started TCP server, port:");
        Form1->Memo1->Lines->Add(Form1->IdTCPServer1->DefaultPort); */
        Form1->IdHTTP1->Get(myurl_start);
        Form1->progbar->Visible = 1;
        Form1->Timer1->Enabled = 1;
        Form1->Button1->Visible = 0;
        Form1->Button2->Visible = 0;
        Form1->Button6->Visible = 1;
}

void http_action_endit() {
        Form1->Timer1->Enabled = 0;
        Form1->IdTCPServer1->Active = 1;
        if(Form1->RadioButton1->Checked) {
/*                Form1->Memo1->Lines->Add("Sending regular announce with size");
                Form1->Memo1->Lines->Add("URL: ");
                Form1->Memo1->Lines->Add(myurl_gimmeh);*/
                Form1->IdHTTP1->Get(myurl_gimmeh);
        }
        else {
/*                Form1->Memo1->Lines->Add("Sending ANNOUNCE with size");
                Form1->Memo1->Lines->Add("URL: ");
                Form1->Memo1->Lines->Add(myurl_putmeh); */
                Form1->IdHTTP1->Get(myurl_putmeh);
        }
        Idglobal::Sleep(random(4000));
/*        Form1->Memo1->Lines->Add("Sending event STOPPED");
        Form1->Memo1->Lines->Add("URL: ");
        Form1->Memo1->Lines->Add(myurl_stop); */
        Form1->IdHTTP1->Get(myurl_stop);
/*        Form1->Memo1->Lines->Add("The end.");
        Form1->Memo1->Lines->Add(Form1->progbar->Position);
        Form1->Memo1->Lines->Add(proc_hash); */
        Form1->Button1->Enabled = TRUE;
        Form1->IdTCPServer1->Active = 0;
        Form1->progbar->Position = 0;
        Form1->progbar->Visible = 0;
        Form1->Button1->Visible = 1;
        Form1->Button1->Enabled = 1;
        Form1->Button2->Visible =1;
        Form1->progbar->Step = 1;
        Form1->Button6->Visible = 0;
}

void __fastcall TForm1::ComboBox1Change(TObject *Sender)
{
        if (Form1->ComboBox1->ItemIndex < 4) {
                Form1->Edit3->Enabled = 0;
                Form1->Edit3->ReadOnly = 1;
                Form1->Edit5->Enabled = 0;
                Form1->Edit5->ReadOnly = 1;
                Form1->Edit4->Enabled = 0;
                Form1->Edit4->ReadOnly = 1;
        }
        else {
                Form1->Edit3->Enabled = 1;
                Form1->Edit3->ReadOnly = 0;
//                Form1->Edit3->Text = "peer_id?";
                Form1->Edit5->Enabled = 1;
                Form1->Edit5->ReadOnly = 0;
//                Form1->Edit5->Text = "user_agent?";
                Form1->Edit4->Enabled = 1;
                Form1->Edit4->ReadOnly = 0;
        }
        if (Form1->ComboBox1->ItemIndex == 0) {
             //   Form1->Edit3->Text = "exbc%009%3D%CF%5B%A3%B2%7E%DC%BC%2E%60%C9%11%FB%E6";        0.57
             //   Form1->Edit3->Text = "%2DBC0059%2D%1AZg%A0%B5N%EE%AE%1A%E5v%D6";                  0.59
             //   Form1->Edit3->Text = "%2DBC0060%2Da%C98%8D%13h%95%B3%A0%A4%15%FA";                  0.60
                Form1->Edit3->Text = genpeerid(Form1->ComboBox1->ItemIndex);
                Form1->Edit5->Text = "BitTorrent/3.4.2";
                Form1->Edit4->Text = genkey(Form1->ComboBox1->ItemIndex);
                Form1->IdHTTP1->ProtocolVersion = pv1_0;
                Form1->IdHTTP1->Request->Accept = "";
                Form1->IdHTTP1->Request->AcceptEncoding = "gzip, deflate";
                Form1->IdHTTP1->Request->Connection = "close";
                Form1->IdHTTP1->Request->ExtraHeaders->Add("Cache-control: no-cache");
        }
        if (Form1->ComboBox1->ItemIndex == 1) {
             //   Form1->Edit3->Text = "T03C-----RczRkKNwyC2";
             // %2dUT1300%2dl%81%01%01%06%c6%14R%01s%dc%9a - uTorrent
                Form1->Edit3->Text = genpeerid(Form1->ComboBox1->ItemIndex);
                Form1->Edit5->Text = "uTorrent/1300";
                Form1->Edit4->Text = genkey(Form1->ComboBox1->ItemIndex);
                Form1->IdHTTP1->ProtocolVersion = pv1_1;
                Form1->IdHTTP1->Request->Accept = "";
                Form1->IdHTTP1->Request->AcceptEncoding = "gzip";
        }
        if (Form1->ComboBox1->ItemIndex == 2) {
             //   Form1->Edit3->Text = "-AZ2300-k3Hcx9oJF30E";
                Form1->Edit3->Text = genpeerid(Form1->ComboBox1->ItemIndex);
                Form1->Edit5->Text = "Azureus 2.3.0.6;Windows XP;Java 1.5.0_04";
                Form1->Edit4->Text = genkey(Form1->ComboBox1->ItemIndex);
                Form1->IdHTTP1->ProtocolVersion = pv1_1;
                Form1->IdHTTP1->Request->Accept = "text/html, image/gif, image/jpeg, *; q=.2, */*; q=.2";
                Form1->IdHTTP1->Request->AcceptEncoding = "gzip";
                Form1->IdHTTP1->Request->ContentType = "application/x-www-form-urlencoded";
        }
        if (Form1->ComboBox1->ItemIndex == 3) {
//                Form1->Edit3->Text = "A301-----YIOHYeNf4CI";
                Form1->Edit3->Text = genpeerid(Form1->ComboBox1->ItemIndex);
                Form1->Edit5->Text = "ABC 3.01/ABC-3.0.1";
                Form1->Edit4->Text = genkey(Form1->ComboBox1->ItemIndex);
                Form1->IdHTTP1->ProtocolVersion = pv1_1;
        }

}
//---------------------------------------------------------------------------
void __fastcall TForm1::FormCreate(TObject *Sender)
{
        Form1->BringToFront();
        unsigned int randport;
        randomize();
        randport = random(64510) + 1024;
        Form1->Edit8->Text = IntToStr(randport); 
//        DecimalSeparator = ","[0];
}
//---------------------------------------------------------------------------

void __fastcall TForm1::Button2Click(TObject *Sender)
{
        Application->Terminate();
}
//---------------------------------------------------------------------------

void __fastcall TForm1::Button1Click(TObject *Sender)
{
        IdHTTP1->Request->UserAgent = Form1->Edit5->Text;
        if(Form1->Edit2->Text == "" || Form1->Edit1->Text == "http://" || Form1->Edit6->Text == "" || Form1->Edit3->Text == "peer_id" || Form1->Edit5->Text == "http_user_agent" || Form1->Edit2->Text.ToIntDef(-1) < 0 ) {
                ShowMessage("Not all required fields filled (correctly).");
        }
        else http_action();

}
//---------------------------------------------------------------------------

void __fastcall TForm1::Button3Click(TObject *Sender)
{
Form1->Height = 537;
//Form1->Memo1->Enabled = TRUE;
//Form1->Button3->Enabled = FALSE;
//Form1->Memo1->Visible = TRUE;
}
//---------------------------------------------------------------------------

void __fastcall TForm1::Button4Click(TObject *Sender)
{
Application->MessageBox("RatioMaker - v0.5.1\n\nCopyright moofdev.org <dev@moofdev.org>\n\nthx to herhe pt ajutor\nthx to Obi pt testing\nthx to 00de.de forum for suggestions & feedback\nBinaries and source code are available at:\n\thttp://www.moofdev.org/\n\nRatioMaker is now licensed under the BSD license,\nyou can find it's text in the nfo file.","ab00t",MB_OK);
}
//---------------------------------------------------------------------------

void __fastcall TForm1::Button5Click(TObject *Sender)
{
Application->MessageBox("\
RatioMaker:\n\
\n\
Tracker: URL to tracker\'s announce, including any passkey\n\
Info hash: The hash of a torrent that the selected tracker serves\n\
Total bytes: The torrent\'s size, in bytes.\n\
\n\
Use TorrentSpy on any .torrent downloaded from any site to\n\
determine the tracker's complete URL, infohash and total bytes.\n\n\
You cannot use fractional values any more, they're not needed since\n\
the app adds a random amount of bytes to your value (so as not to\n\
have exact 1024*1024 multiples).","Quick help",MB_OK);
}
//---------------------------------------------------------------------------

void __fastcall TForm1::Timer1Timer(TObject *Sender)
{
        Form1->progbar->StepIt();
        if(Form1->progbar->Position == 300) http_action_endit();
        if(Form1->progbar->Position >290 && Form1->progbar->Position < 300 && Form1->progbar->Step == 10) http_action_endit();
        if(Form1->progbar->Position == 10)  Form1->IdTCPServer1->Active = 0;
}

//---------------------------------------------------------------------------



void __fastcall TForm1::Button6Click(TObject *Sender)
{
        Form1->Button6->Visible = 0;
        Form1->progbar->Step = 10;
}
//---------------------------------------------------------------------------


void __fastcall TForm1::CheckBox1Click(TObject *Sender)
{
        if(Form1->CheckBox1->Checked) {
                Form1->IdHTTP1->SocksInfo->Authentication = saUsernamePassword;
                Form1->Edit11->Enabled = 1;
                Form1->Edit12->Enabled = 1;
        }
        else {
                Form1->IdHTTP1->SocksInfo->Authentication = saNoAuthentication;
                Form1->Edit11->Enabled = 1;
                Form1->Edit12->Enabled = 1;
        }
}
//---------------------------------------------------------------------------

void __fastcall TForm1::RadioButton5Click(TObject *Sender)
{
        Form1->IdHTTP1->SocksInfo->Version = svSocks5;
        Form1->CheckBox1->Enabled = 1;
        Form1->Edit9->Enabled = 1;
        Form1->Edit10->Enabled = 1;
}
//---------------------------------------------------------------------------

void __fastcall TForm1::RadioButton4Click(TObject *Sender)
{
        Form1->IdHTTP1->SocksInfo->Version = svSocks4A;
        Form1->CheckBox1->Enabled = 0;
        Form1->Edit9->Enabled = 1;
        Form1->Edit10->Enabled = 1;
}
//---------------------------------------------------------------------------

void __fastcall TForm1::RadioButton3Click(TObject *Sender)
{
        Form1->IdHTTP1->SocksInfo->Version = svNoSocks;
        Form1->CheckBox1->Enabled = 0;
        Form1->Edit9->Enabled = 0;
        Form1->Edit10->Enabled = 0;
}
//---------------------------------------------------------------------------

void __fastcall TForm1::Edit9Change(TObject *Sender)
{
        Form1->IdHTTP1->SocksInfo->Host = Form1->Edit9->Text;        
}
//---------------------------------------------------------------------------

void __fastcall TForm1::Edit10Change(TObject *Sender)
{
        Form1->IdHTTP1->SocksInfo->Port = Form1->Edit10->Text.ToInt();
}
//---------------------------------------------------------------------------

void __fastcall TForm1::Edit12Change(TObject *Sender)
{
        Form1->IdHTTP1->SocksInfo->Password = Form1->Edit12->Text;        
}
//---------------------------------------------------------------------------

void __fastcall TForm1::Edit11Change(TObject *Sender)
{
        Form1->IdHTTP1->SocksInfo->UserID = Form1->Edit11->Text;        
}
//---------------------------------------------------------------------------

void __fastcall TForm1::Label5Click(TObject *Sender)
{
ShellExecute(NULL,"open","http://www.moofdev.org/",NULL,NULL,SW_SHOW);
}
//---------------------------------------------------------------------------

