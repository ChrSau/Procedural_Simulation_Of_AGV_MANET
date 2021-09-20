clear all;
close all;
clc;

[s,F,nagv,nap,T,C,P] = LoadFile('TestFiles/001.csv');

subplot(1,2,1);
plot(T,C);
xlabel('Time in s');
ylabel('Percentage of AGVs connected');
subplot(1,2,2);
plot(T,P);
xlabel('Time in s');
ylabel('Average performance in T/h/AGV');