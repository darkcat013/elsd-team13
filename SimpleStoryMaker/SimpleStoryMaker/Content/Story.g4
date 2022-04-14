grammar Story;

program: text* EOF;

text: STRING ':' input ';';

input: CHAR;

CHAR: [a-zA-Z0-9];

STRING: ('"' ~'"'* '"');

WS: [ \t\r\n]+ -> skip;
