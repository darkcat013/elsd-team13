grammar Story;

program: player start scene* end EOF;

player: 'PLAYER''{'attribute*'}';

attribute: IDENTIFIER ':' expression';';

expression
    : literal                           #literalExpression
    | variableCall                      #variableCallExpression
    | '(' expression ')'                #paranthesizedExpression
    | '!' expression                    #notExpression
    | expression MULTOP expression      #multiplicativeExpression
    | expression ADDOP expression       #additiveExpression
    | expression COMPAREOP expression   #comparisonExpression
    | expression BOOLOP expression      #booleanExpression
    ;

start: SCENE START'{'write (choices | goTo)'}';
end: SCENE END'{'write'}';
scene: SCENE IDENTIFIER'{'write (choices | goTo)'}';

write: 'write'':'expression';';

choices:'choices''{'choice+'}';

choice: IDENTIFIER'{'choiceText goTo'}';

choiceText: 'text'':'expression';';

goTo: 'goto'':'(IDENTIFIER | START | END)';';

variableCall: IDENTIFIER('.'IDENTIFIER)*;

literal: number | STRING | BOOL;

number: INT | INT'.'INT;

MULTOP: '*' | '/';
ADDOP: '+' | '-';
COMPAREOP: '==' | '!=' | '>' | '<' | '>=' | '<=';
BOOLOP: 'and' | 'or';

INT: [0-9]+;
STRING: ('"' ~'"'* '"') | ('\'' ~'\''* '\'');
BOOL: 'true' | 'false';

WS: [ \t\r\n]+ -> skip;
START: 'START';
END: 'END';
SCENE: 'SCENE ';
IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_]*;