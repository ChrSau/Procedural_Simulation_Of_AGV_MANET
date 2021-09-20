function [s,F,nagv,nap,T,C,P] = LoadFile( filename )

%% Raw import

delimiter = ';';
startRow = 1;
endRow = inf;
formatSpec = '%s%s%s%[^\n\r]';
fileID = fopen(filename,'r');
textscan(fileID, '%[^\n\r]', startRow(1)-1, 'ReturnOnError', false);
dataArray = textscan(fileID, formatSpec, endRow(1)-startRow(1)+1, 'Delimiter', delimiter, 'ReturnOnError', false);
for block=2:length(startRow)
    frewind(fileID);
    textscan(fileID, '%[^\n\r]', startRow(block)-1, 'ReturnOnError', false);
    dataArrayBlock = textscan(fileID, formatSpec, endRow(block)-startRow(block)+1, 'Delimiter', delimiter, 'ReturnOnError', false);
    for col=1:length(dataArray)
        dataArray{col} = [dataArray{col};dataArrayBlock{col}];
    end
end
fclose(fileID);
raw = repmat({''},length(dataArray{1}),length(dataArray)-1);
for col=1:length(dataArray)-1
    raw(1:length(dataArray{col}),col) = dataArray{col};
end
numericData = NaN(size(dataArray{1},1),size(dataArray,2));
for col=[1,2,3]
    rawData = dataArray{col};
    for row=1:size(rawData, 1);
        regexstr = '(?<prefix>.*?)(?<numbers>([-]*(\d+[\.]*)+[\,]{0,1}\d*[eEdD]{0,1}[-+]*\d*[i]{0,1})|([-]*(\d+[\.]*)*[\,]{1,1}\d+[eEdD]{0,1}[-+]*\d*[i]{0,1}))(?<suffix>.*)';
        try
            result = regexp(rawData{row}, regexstr, 'names');
            numbers = result.numbers;
            invalidThousandsSeparator = false;
            if any(numbers=='.');
                thousandsRegExp = '^\d+?(\.\d{3})*\,{0,1}\d*$';
                if isempty(regexp(thousandsRegExp, '.', 'once'));
                    numbers = NaN;
                    invalidThousandsSeparator = true;
                end
            end
            if ~invalidThousandsSeparator;
                numbers = strrep(numbers, '.', '');
                numbers = strrep(numbers, ',', '.');
                numbers = textscan(numbers, '%f');
                numericData(row, col) = numbers{1};
                raw{row, col} = numbers{1};
            end
        catch me
        end
    end
end
R = cellfun(@(x) ~isnumeric(x) && ~islogical(x),raw);
raw(R) = {NaN};
rawImport = cell2mat(raw);

%% Process

s = rawImport(1,2);
F = rawImport(2,2:3);
nagv = rawImport(3,2);
nap = rawImport(4,2);
T = rawImport(6:end,1);
C = rawImport(6:end,2);
P = rawImport(6:end,3);

end

