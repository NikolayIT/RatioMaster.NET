//---------------------------------------------------------------------------

#ifndef RatioMakerH
#define RatioMakerH
//---------------------------------------------------------------------------
#include <Classes.hpp>
#include <Controls.hpp>
#include <StdCtrls.hpp>
#include <Forms.hpp>
#include <ExtCtrls.hpp>
#include <IdBaseComponent.hpp>
#include <IdComponent.hpp>
#include <IdHTTP.hpp>
#include <IdTCPClient.hpp>
#include <IdTCPConnection.hpp>
#include <ComCtrls.hpp>
#include <IdTCPServer.hpp>
//---------------------------------------------------------------------------
class TForm1 : public TForm
{
__published:	// IDE-managed Components
        TPanel *Panel1;
        TGroupBox *GroupBox1;
        TEdit *Edit1;
        TGroupBox *GroupBox2;
        TEdit *Edit2;
        TGroupBox *GroupBox3;
        TRadioButton *RadioButton1;
        TRadioButton *RadioButton2;
        TGroupBox *GroupBox4;
        TComboBox *ComboBox1;
        TEdit *Edit3;
        TGroupBox *GroupBox5;
        TEdit *Edit4;
        TEdit *Edit5;
        TButton *Button1;
        TButton *Button2;
        TIdHTTP *IdHTTP1;
        TGroupBox *GroupBox6;
        TEdit *Edit6;
        TButton *Button4;
        TButton *Button5;
        TGroupBox *GroupBox7;
        TEdit *Edit7;
        TProgressBar *progbar;
        TButton *Button6;
        TTimer *Timer1;
        TGroupBox *GroupBox8;
        TEdit *Edit8;
        TIdTCPServer *IdTCPServer1;
        TGroupBox *GroupBox9;
        TRadioButton *RadioButton3;
        TRadioButton *RadioButton4;
        TRadioButton *RadioButton5;
        TCheckBox *CheckBox1;
        TEdit *Edit9;
        TEdit *Edit10;
        TLabel *Label1;
        TLabel *Label2;
        TEdit *Edit11;
        TEdit *Edit12;
        TLabel *Label3;
        TLabel *Label4;
        TLabel *Label5;
        void __fastcall ComboBox1Change(TObject *Sender);
        void __fastcall FormCreate(TObject *Sender);
        void __fastcall Button2Click(TObject *Sender);
        void __fastcall Button1Click(TObject *Sender);
        void __fastcall Button3Click(TObject *Sender);
        void __fastcall Button4Click(TObject *Sender);
        void __fastcall Button5Click(TObject *Sender);
        void __fastcall Timer1Timer(TObject *Sender);
        void __fastcall Button6Click(TObject *Sender);
        void __fastcall CheckBox1Click(TObject *Sender);
        void __fastcall RadioButton5Click(TObject *Sender);
        void __fastcall RadioButton4Click(TObject *Sender);
        void __fastcall RadioButton3Click(TObject *Sender);
        void __fastcall Edit9Change(TObject *Sender);
        void __fastcall Edit10Change(TObject *Sender);
        void __fastcall Edit12Change(TObject *Sender);
        void __fastcall Edit11Change(TObject *Sender);
        void __fastcall Label5Click(TObject *Sender);
private:	// User declarations
public:		// User declarations
        __fastcall TForm1(TComponent* Owner);
};
//---------------------------------------------------------------------------
extern PACKAGE TForm1 *Form1;
//---------------------------------------------------------------------------
#endif
