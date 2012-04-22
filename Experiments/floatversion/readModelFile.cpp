#include <iostream>
#include <fstream>
#include <string>
#include <map>
#include <algorithm>
#include "stdafx.h"
#include "dataanalysis.h"

using namespace std;


void readsvmfile(float * container, int lines, int stride, char * filename) {
  int counter = 0;
  string line, part;
  int feature;
  float label, value;
  
  ifstream ifile (filename);
  
  if (ifile.is_open()) {
    while (ifile.good()) {
      if (counter == lines) break;
      
      getline(ifile, line);
     
      sscanf(line.c_str(), "%f", &label);
      if (line.find(' ') == string::npos) line = "";
      else line = line.substr(line.find(' ') + 1); 
      
      // XXX: Fix this for the general case.
      if (label == -1) label = 0;
      container[(counter*stride) + stride - 1] = label; 
      
      while (!line.empty()) {
        if (line.find(' ') == string::npos) part = line;
        else part = line.substr(0, line.find(' '));
        
        sscanf(part.c_str(), "%d:%f", &feature, &value);
	
        //printf("Part: %s, Line: %s\n", part.c_str(), line.c_str());
        
        //printf("Setting [%d] = %f...", feature, value);
        //fflush(stdout);
        container[(counter*stride) + feature - 1] = value;
        //printf("set.\n");

        if (part.length() == line.length()) line = "";
        else line = line.substr(part.length() + 1);  
       
      }
      
      if (counter % 1000 == 0) {
        float percent = (float)counter/(float)lines;
        printf("\tDone reading %f (%d/%d)\n", percent, counter, lines);
      }
      
      counter ++;
    }
  }
}

int _tmain(int argc, _TCHAR* argv[])
{
  cout<< "Hello World" <<endl;
  char i;
  cin>>i; 
  return 0;
	if (argc != 4) {
    cout << "Usage: ./<exec> <nfeatures> <ntraining images> <ntrees>" << endl;
    exit(1);
  }
  
  cout << "Welcome." << endl;
  
  alglib::dfreport rep;
  alglib::decisionforest df;
  alglib::ae_int_t info; 
  
  cout << "Created forest, and report." << endl;
  
  
  /************************************/
  /* EXPERIMENT SPECIFIC PARAMETERS   */
  /************************************/
  int nfeatures = atoi(argv[1]); 
  int ntrain_images = atoi(argv[2]);
  int ntrees = atoi(argv[3]);
  /************************************/
  /************************************/
   
  int ntest = 103280;
  char model_file [50], test_file [50], results_file [50];
  
  sprintf(model_file, "RF.%d.%d.%d.model.prune", nfeatures, ntrain_images, ntrees);
  sprintf(test_file, "F%d.test.features.txt", nfeatures);
  sprintf(results_file, "Accuracy.%s.txt", model_file);
  
  cout << "Reading in test file." << endl;
  float * _test = new float [ntest*(nfeatures + 1)];
  alglib::real_2d_array test;
  readsvmfile(_test, ntest, nfeatures + 1, test_file);
  test.setcontent(ntest, nfeatures + 1, _test);
  delete [] _test;
  
  cout << "Unserializing model file." << endl;
  ifstream ifile (model_file);
  string model, line;
  if (ifile.is_open()) {
    while (ifile.good()) {
      getline(ifile, line);
      model += line;
    }
    ifile.close();
  }
  

  alglib::dfunserialize(model, df);
  
  cout << "Testing." << endl;
  float error = alglib::dfavgrelerror(df, test, ntest);
  printf("Average error is %f\n", error);
  
  ofstream results_ofile (results_file);
  results_ofile << error << endl;
  results_ofile.close();
  return 0;
}
