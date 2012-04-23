// Test.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "..//..//dataanalysis.h"

#include <iostream>
#include <fstream>
  #include <streambuf>
using namespace std;
int _tmain(int argc, _TCHAR* argv[])
{
	//cout<< "Hello World"<<endl;
	/*
	if (argc != 4) {
    cout << "Usage: ./<exec> <nfeatures> <ntraining images> <ntrees>" << endl;
    exit(1);
  }
  
 */ 
 // alglib::dfreport rep;
  alglib::decisionforest df;
  //alglib::ae_int_t info; 
 
 //char model_file [50]=; 
  string model_file;
  if (argc==1){
	  model_file = "";
	  // model_file= "C:\\Users\\Michael Zhang\\Desktop\\HandGestureRecognition\\ColorGlove\\Data\\RF.demo.1000.800.1.model";
  }
  else
  {
     model_file = string(argv[1]); 
  }
	 //string model_file(argv[1]);

 ifstream t(model_file);
 string str;
 t.seekg(0, std::ios::end);   
 str.reserve(t.tellg());
 t.seekg(0, std::ios::beg);

 str.assign((std::istreambuf_iterator<char>(t)),
            std::istreambuf_iterator<char>());
  

  alglib::dfunserialize(str, df);
  cout<< endl<< "Successfully read the model file!" <<endl;
  /*
  char i;
  cin >> i;
  */
  return 0;
}

