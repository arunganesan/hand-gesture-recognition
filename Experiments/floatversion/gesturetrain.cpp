#include <iostream>
#include <fstream>
#include <string>
#include <map>
#include <algorithm>

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

int main (int argc, char * argv []) {
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
  
  map<int, int> line_counts;
  line_counts[10] = 19999;
  line_counts[50] = 99439;
  line_counts[100] = 198662;
  line_counts[150] = 298446;
  line_counts[200] = 398799;
  line_counts[250] = 496847;
  line_counts[300] = 596372;
  line_counts[350] = 695672;
  
  float r = 0.66;
  int nclasses = 5;
  int ntest = 103280;
  int ntrain = line_counts[ntrain_images];
  char model_file [50], training_file [50], test_file [50], results_file [50];
  
  sprintf(model_file, "RF.%d.%d.%d.model", nfeatures, ntrain_images, ntrees);
  sprintf(training_file, "F%d.%d.features.txt", nfeatures, ntrain_images);
  sprintf(test_file, "F%d.test.features.txt", nfeatures);
  sprintf(results_file, "Results.%d.%d.%d.txt", nfeatures, ntrain_images, ntrees);

  cout << "Reading in training file." << endl;
  float * _train = new float [ntrain*(nfeatures + 1)];
  alglib::real_2d_array train;
  readsvmfile(_train, ntrain, nfeatures + 1, training_file);
  train.setcontent(ntrain, nfeatures + 1, _train);
  delete [] _train;

  cout << "Reading in test file." << endl;
  float * _test = new float [ntest*(nfeatures + 1)];
  alglib::real_2d_array test;
  readsvmfile(_test, ntest, nfeatures + 1, test_file);
  test.setcontent(ntest, nfeatures + 1, _test);
  delete [] _test;
  
  cout << "Training." << endl;
  alglib::dfbuildrandomdecisionforest(train, ntrain, nfeatures, nclasses, ntrees, r, info, df, rep); 
  
  cout << "Testing." << endl;
  float error = alglib::dfavgrelerror(df, test, ntest);
  printf("Average error is %f\n", error);
  
  ofstream results_ofile (results_file);
  results_ofile << error << endl;
  results_ofile.close();

  string serialized;
  alglib::dfserialize(df, serialized);
  ofstream ofile (model_file);
  ofile << serialized;
  ofile.close();
  
  return 0;
}
