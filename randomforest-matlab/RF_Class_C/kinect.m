%% Read data
%run plethora of tests
clc
close all

%compile everything
% if strcmpi(computer,'PCWIN') |strcmpi(computer,'PCWIN64')
%    compile_windows
% else
%    compile_linux
% end

total_train_time=0;
total_test_time=0;

% %load the twonorm dataset 
% load data/twonorm
%  
% %modify so that training data is NxD and labels are Nx1, where N=#of
% %examples, D=# of features
% 
% X = inputs';
% Y = outputs;
% 
% [N D] =size(X);
% %randomly split into 250 examples for training and 50 for testing
% randvector = randperm(N);
% 
% X_trn = X(randvector(1:250),:);
% Y_trn = Y(randvector(1:250));
% X_tst = X(randvector(251:end),:);
% Y_tst = Y(randvector(251:end));

%data_path = '/home/caoxiezh/Kinect/Data/FeatureVectorBlue149.txt'; 
data_path = '/home/caoxiezh/Kinect/Data/FeatureVectorBlue73.txt'; 
%data_path = '/home/caoxiezh/liblinear++/heart_scale'; 


[Y_trn, X_trn] = libsvmread([data_path '.train']);
X_trn = full(X_trn);
[Y_tst, X_tst] = libsvmread([data_path '.test']);
X_tst = full(X_tst);

%% Train data
% example 3:  set to 100 trees, mtry = 2
    model = classRF_train(X_trn,Y_trn, 3,1);
    %Y_hat = classRF_predict(X_tst,model);
    Y_hat = classRF_predict(X_tst,model);
    %fprintf('\nexample : training error rate %f\n',   length(find(Y_hat~=Y_tst))/length(Y_tst));
    fprintf('\nexample : testing error rate %f\n',   length(find(Y_hat~=Y_tst))/length(Y_tst));
    